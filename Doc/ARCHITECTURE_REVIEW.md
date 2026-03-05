### Đánh giá kiến trúc ServerForUnity

---

## 1. Mục đích & Phạm vi

- **Mục đích**:  
  Đánh giá kiến trúc hiện tại của dự án `ServerForUnity` cho game Caro/Gomoku, tập trung vào:
  - **Những lỗi sai nghiêm trọng / rủi ro lớn**
  - **Mức độ phù hợp với kiến trúc Game Online (server authoritative, matchmaking, session, scale, v.v.)**
  - **Đề xuất cải thiện cụ thể**

- **Phạm vi**:  
  Dựa trên nội dung trong `ARCHITECTURE.md`. Những điểm “chưa thấy/không được đề cập” được coi là rủi ro tiềm ẩn.

---

## 2. Tổng quan đánh giá

- **Mức độ hoàn thiện**:  
  Kiến trúc đã được mô tả khá rõ ràng: chia lớp Network, GameLogic, Database, Shared, có flow cho Login, Matchmaking, Ping, Match, v.v.  
- **Định hướng**:  
  Đây là một **TCP game server nhỏ, single-process, in-memory state**, phù hợp cho:
  - Demo
  - Prototype
  - Game nhỏ, ít người, 1 server đơn
- **Hạn chế chính**:  
  Chưa phù hợp cho:
  - Số lượng người chơi lớn
  - Yêu cầu bảo mật tài khoản (mật khẩu), chống gian lận
  - Khả năng scale ngang nhiều server

---

## 3. Những điểm mạnh kiến trúc

- **Tách dự án rõ ràng**
  - `Server`, `Client`, `Shared`, `Test` được phân chia tốt.
  - `Shared` gom các phần chung: `Network`, `GameLogic` command, `Common` (logger, pool, service locator…).

- **Network layer tương đối chuẩn**
  - Gói tin `Length (4 bytes) + Header (4 bytes) + Payload` là format rất phổ biến, dễ mở rộng.
  - Có `PacketDispatcher` + `IPacketHandler` → dễ thêm command mới.

- **Game flow rõ ràng**
  - Có flow cho:
    - Login
    - Find_Match
    - Execute_Turn
    - Ping/Pong & Disconnect
  - Match được model hóa bằng `Match` với 2 player, board, trạng thái lượt, win-check.

- **Sử dụng pattern quen thuộc**
  - **Singleton** cho `SessionManager`, `PacketDispatcher`.
  - **Repository** cho database (`IPlayerRepository`, `SQLitePlayerRepository`).
  - **Object pool** cho `Session` (tối ưu GC cho kết nối ngắn sống).

---

## 4. Các lỗi sai nghiêm trọng / rủi ro lớn

### 4.1. Bảo mật tài khoản (mật khẩu lưu dạng TEXT thuần)

- **Mô tả**:
  - Schema SQLite:

    ```sql
    Password TEXT NOT NULL
    ```

  - Tài liệu không nhắc tới:
    - Hash mật khẩu
    - Salt
    - Thuật toán băm (BCrypt, PBKDF2, Argon2, …)

- **Rủi ro**:
  - Lưu mật khẩu dạng plain text là **rất nghiêm trọng** nếu dùng cho bất kỳ môi trường thật nào.
  - Bị lộ DB = lộ toàn bộ mật khẩu người dùng.

- **Đề xuất**:
  - Bắt buộc thay `Password TEXT` thành:
    - `PasswordHash TEXT`
    - `Salt TEXT` (hoặc dùng thuật toán đã tích hợp salt nội bộ như BCrypt).
  - Thực hiện hash mật khẩu khi:
    - Đăng ký
    - Đổi mật khẩu
  - So sánh bằng cách băm lại input rồi so với hash lưu trong DB.

---

### 4.2. `Shared` được cấu hình là Exe nhưng dùng như Library

- **Mô tả**:
  - Trong bảng Project: `Shared` ghi chú: “*Shared is configured as Exe but used as a library*”.

- **Rủi ro**:
  - Không phải lỗi logic runtime, nhưng:
    - Làm rối cấu hình build.
    - Có thể gây nhầm lẫn khi mở solution, debugging, publish.
  - Về lâu dài, đây là **một smell cấu hình** nên sửa sớm.

- **Đề xuất**:
  - Chuyển `Shared` sang `Class Library` (project type chuẩn).
  - Đảm bảo `Server`, `Client`, `Test` reference đến `Shared` như một lib bình thường.

---

### 4.3. `IPlayerService` được thiết kế nhưng **chưa đăng ký / chưa hoàn thiện**

- **Mô tả**:
  - Bảng Service Dependencies:

    - `IPlayerRepository` → `SQLitePlayerRepository` (có đăng ký).
    - `IPlayerService` → `PlayerManager` (chưa đăng ký: ✗).

  - `MatchManager` / matchmaking flow có dùng `IPlayerService.GetRandomPlayerByState`.

- **Rủi ro**:
  - Nếu mã nguồn đang dùng `ServiceLocator.GetService<IPlayerService>()`:
    - Không đăng ký service → **Null/Exception** tại runtime.
  - Hoặc server phải bypass interface, gọi trực tiếp `PlayerManager` singleton:
    - Mất lợi ích interface, trói chặt implementation.

- **Đề xuất**:
  - Hoàn thiện DI/ServiceLocator:
    - Đăng ký `IPlayerService` → `PlayerManager` trong `Program.cs`.
  - Hoặc:
    - Bỏ bớt interface nếu không dùng DI, dùng trực tiếp `PlayerManager`, nhưng đây là hướng kém linh hoạt.

---

### 4.4. Kiến trúc session/game state **phụ thuộc chặt vào TCP connection**

- **Mô tả**:
  - Session lifecycle: `Created → Start → ReceiveLoopAsync → OnClosed → Release to pool`.
  - Player binding: `session.BindPlayer(player)` sau khi login.
  - Player state: `Offline → Online → FindMatch → InMatch → Online`.

- **Rủi ro**:
  - **Không có concept session token / auth token** ngoài TCP connection:
    - Nếu client mất kết nối mạng tạm thời:
      - `Session.Close()` → Match xử lý winner by disconnect.
      - Không có cơ chế **reconnect vào cùng match**.
  - Phù hợp cho demo, nhưng:
    - Với game online thực tế, mạng di động rất hay rớt.
    - Người chơi sẽ mất trận chỉ vì mất kết nối ngắn.

- **Đề xuất**:
  - Thiết kế thêm:
    - `AuthToken` hoặc `SessionId` logic (không gắn chết vào TCP connection).
    - Luồng `ReconnectMatch`:
      - Client gửi token + player id.
      - Server map lại vào match nếu còn tồn tại.

---

### 4.5. Khả năng scale & kiến trúc đơn node

- **Mô tả**:
  - Server là 1 process duy nhất:
    - Match, Session, Player state đều in-memory.
  - Database: SQLite (file-based, single DB file).

- **Rủi ro**:
  - **Không scale ngang**:
    - Không thể dễ dàng chạy nhiều server xử lý chung một pool người chơi.
  - SQLite:
    - Hạn chế concurrency.
    - Không phù hợp cho traffic lớn, nhiều truy vấn ghi đồng thời.
  - Trong bối cảnh “Game Online”:
    - Nếu mục tiêu chỉ là vài chục / vài trăm user test → OK.
    - Nếu mục tiêu hàng ngàn user → kiến trúc này **không đáp ứng**.

- **Đề xuất**:
  - Ngắn hạn:
    - Rõ ràng hóa trong doc: “Server đơn, không scale, chỉ dùng cho X user (demo/test)”.
  - Dài hạn:
    - Thay SQLite bằng DB server (PostgreSQL, MySQL, …).
    - Thiết kế:
      - **Gateway server** (nhận kết nối, route tới match server).
      - **Match server / room server** (chứa logic match).
      - Lưu state quan trọng vào DB hoặc cache (Redis) để hỗ trợ scale.

---

### 4.6. Service Locator + nhiều Singleton (khó test & bảo trì)

- **Mô tả**:
  - Sử dụng `ServiceLocator` + `Singleton<T>`.
  - `PacketDispatcher`, `SessionManager` là singleton.

- **Rủi ro**:
  - **Khó unit test**:
    - ServiceLocator là anti-pattern phổ biến vì giấu dependency.
  - Khó chuyển sang kiến trúc phức tạp hơn (DI container chuẩn, multi-instance server).
  - Không phải bug runtime ngay lập tức, nhưng là **technical debt**.

- **Đề xuất**:
  - Về lâu dài:
    - Chuyển dần sang DI chuẩn (`IServiceCollection` trong .NET).
    - Giảm dùng ServiceLocator, giữ lại Singleton chỉ cho những thứ thật sự global, nhưng vẫn inject qua constructor nếu có thể.

---

### 4.7. Thiếu đề cập về validation / chống gian lận

- **Mô tả**:
  - Flow `Execute_Turn(x, y)`:
    - `MatchManager.HandlePlayerExecutedTurn` → `Match.HandlePlayerExecutedTurn` → `CheckLegit → MarkCell → CheckToEndMatch`.
  - Tài liệu không nói rõ:
    - Có kiểm tra người chơi có đúng lượt không?
    - Có kiểm tra tọa độ hợp lệ, cell trống chưa?
    - Có kiểm tra client không gửi “hack turn” (đánh liên tục, đánh ngoài board)?

- **Rủi ro**:
  - Nếu server **không validate đủ mạnh**:
    - Client hack đơn giản có thể:
      - Đánh 2 lượt liên tiếp.
      - Đánh ra ngoài board.
      - Giả mạo trạng thái thắng.

- **Đề xuất**:
  - Đảm bảo tất cả validation được thực hiện **trên server**:
    - Đúng lượt.
    - Vị trí hợp lệ.
    - Cell trống.
    - Trạng thái match chưa kết thúc.
  - Bổ sung mục “Validation & Anti-cheat” trong tài liệu kiến trúc.

---

## 5. Đánh giá mức độ phù hợp với kiến trúc Game Online

### 5.1. Những gì đang **phù hợp**

- **Server-authoritative cho logic game**  
  Nếu toàn bộ win-check, lượt đi, board đều được xử lý trên server (như doc mô tả), đây là hướng đúng cho game online.

- **Chia layer Network / GameLogic / Database / Shared**
  - Network: `Packet`, `Session`, `PacketDispatcher`.
  - GameLogic: `PlayerManager`, `MatchManager`, `Match`.
  - DB: `IPlayerRepository` + SQLite.
  - Shared commands c2s/s2c chuẩn hóa giữa Client/Server.  
  Điều này khá giống kiến trúc cơ bản của nhiều game online nhỏ.

- **Ping/Pong & Disconnect handling**
  - Có cơ chế health check → đúng hướng cho game real-time / near real-time.

- **Matchmaking đơn giản**
  - Sử dụng `GetRandomPlayerByState(EPlayerState.FindMatch)` → phù hợp cho:
    - Game nhỏ, 1vs1, không yêu cầu ranking/phân cấp.

### 5.2. Những gì **chưa đủ** nếu coi là “Game Online” ở quy mô lớn

- **Không có lớp Gateway / Login server riêng**
  - Toàn bộ login + match + session trong 1 process.
  - Không phân chia:
    - Authentication service
    - Matchmaking service
    - Game/match instance service

- **Không có cơ chế multi-server / multi-region**
  - Không đề cập:
    - Sharding người chơi.
    - Routing theo region.
    - Đồng bộ state giữa server.

- **Storage & state chưa thân thiện với scale**
  - SQLite cho tài khoản.
  - Match state in-memory, không persistence (nếu server crash, toàn bộ match mất).

- **Chưa đề cập monitoring / metrics**
  - Không thấy nói tới:
    - Log tập trung.
    - Metrics (số session, match, ping latency).
    - Alert khi server quá tải.

---

## 6. Đề xuất cải thiện kiến trúc

### 6.1. Ngắn hạn (cho phiên bản demo / học tập)

- **Bảo mật mật khẩu**
  - Thêm hash + salt.
  - Cập nhật schema và logic login/signup.

- **Sửa cấu hình project `Shared`**
  - Chuyển thành `Class Library`.

- **Hoàn thiện DI/ServiceLocator tối thiểu**
  - Đăng ký đầy đủ `IPlayerService` → `PlayerManager`.
  - Đảm bảo không có chỗ nào `GetService<T>` trả null.

- **Rõ ràng hóa tài liệu**
  - Thêm mục:
    - Phạm vi: “Server single-node, chỉ dùng cho X user, không production”.
    - Validation & anti-cheat (ngắn gọn nhưng rõ: server validate toàn bộ).

### 6.2. Trung hạn (khi muốn hỗ trợ nhiều người chơi hơn)

- **Tách logic thành service rõ ràng hơn**
  - Trong cùng process nhưng phân tách module:
    - Auth module
    - Matchmaking module
    - Match module

- **Chuẩn hóa DI**
  - Giảm phụ thuộc vào ServiceLocator.
  - Sử dụng `Microsoft.Extensions.DependencyInjection` để dễ test và mở rộng.

- **Cải thiện database**
  - Nếu số lượng user / request tăng:
    - Xem xét chuyển dần sang DB server (Postgres, MySQL).

### 6.3. Dài hạn (nếu mục tiêu là Game Online thực thụ quy mô lớn)

- **Thêm Gateway server**
  - Xử lý connect/disconnect.
  - Terminate TCP, chuyển logic request thành message nội bộ (gRPC, message queue…).

- **Tách Match server / Room server**
  - Mỗi nhóm match do 1 server đảm nhiệm.
  - Có cơ chế phân phối match mới giữa các server theo tải.

- **Thêm cơ chế Reconnect & Session token**
  - Cho phép client reconnect vào match nếu rớt mạng trong thời gian ngắn.

- **Monitoring & metrics**
  - Thu thập log & metric (Prometheus, Grafana, …) để theo dõi tình trạng server.

---

## 7. Kết luận

- **Tổng thể**, kiến trúc hiện tại **phù hợp với một game server prototype nhỏ**, có cấu trúc rõ ràng và mô tả đầy đủ các flow chính (login, matchmaking, match, ping/pong).  
- **Các vấn đề nghiêm trọng nhất** cần xử lý sớm:
  - Lưu mật khẩu dạng plain text.
  - `Shared` cấu hình sai loại project.
  - `IPlayerService` chưa được đăng ký/hoàn thiện → dễ gây lỗi runtime.
- **Về kiến trúc Game Online**, thiết kế hiện tại là nền tảng tốt cho game nhỏ 1 server, nhưng **chưa đủ** cho game online quy mô lớn (scale, bảo mật cao, chống gian lận, multi-server).  
- Khi bạn cập nhật thêm code hoặc tài liệu (ví dụ `DEVELOPMENT_PLAN.md`), có thể tiếp tục refine kiến trúc theo các đề xuất trên.

