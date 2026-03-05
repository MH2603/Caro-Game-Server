### ServerForUnity – Architecture v2 (Proposed)

---

## 1. Purpose & Audience

- **Purpose**:  
  Mô tả kiến trúc **mục tiêu** (v2) cho dự án `ServerForUnity` – server game Caro/Gomoku – sau khi đã áp dụng các đề xuất trong `ARCHITECTURE_REVIEW.md`. Tài liệu này là chuẩn tham chiếu khi:
  - Thiết kế / refactor code.
  - Thêm tính năng mới (matchmaking, reconnect, bảo mật…).
  - Đánh giá mức độ sẵn sàng cho scale / production nhỏ.

- **Audience**:  
  - Developer backend .NET tham gia vào `ServerForUnity`.
  - QA / Tester muốn hiểu luồng xử lý.
  - Người thiết kế hệ thống muốn xem kiến trúc game server nhỏ (single node).

- **Scope**:  
  - Single-node TCP game server (1 process, in-memory state).
  - Bảo mật mức cơ bản (password hashing).
  - Chưa bao gồm kiến trúc multi-server / multi-region (được đề cập ở phần định hướng tương lai).

---

## 2. High-level Overview

`ServerForUnity` là một TCP game server dùng cho game Caro/Gomoku 1vs1, cung cấp:

- **Authentication** – Đăng ký, đăng nhập với mật khẩu đã hash.
- **Session management** – Quản lý kết nối TCP, session logic, ping/pong, timeout.
- **Matchmaking** – Tìm đối thủ ngẫu nhiên trong trạng thái FindMatch.
- **Game logic** – Quản lý `Match` (board, lượt đi, win-check, kết thúc trận).
- **Persistence** – Lưu thông tin player vào SQLite với schema an toàn hơn.

Server là **server-authoritative**:

- Client chỉ gửi **ý định** (ví dụ: “đánh tại (x, y)”).
- Server validate, cập nhật state, tự tính toán thắng/thua/hòa rồi gửi kết quả xuống client.

---

## 3. Solution & Project Structure

### 3.1. Project Layout

```text
ServerForUnity/
├── Server/              # Game server (console app)
├── Client/              # Test client (console app)
├── Shared/              # Class Library: shared network, commands, models, common utils
├── Test/                # Test project (unit/integration)
└── Server For Unity.sln
```

### 3.2. Project Types & Dependencies

| Project     | Type          | Depends on |
|------------|---------------|------------|
| `Server`   | Console App   | `Shared`   |
| `Client`   | Console App   | `Shared`   |
| `Shared`   | Class Library | –          |
| `Test`     | Test Project  | `Server`, `Client`, `Shared` |

**Thay đổi chính so với v1**:

- `Shared` chuyển từ Exe sang **Class Library** để phù hợp với vai trò thư viện dùng chung.

---

## 4. Logical Modules & Folders

### 4.1. Server Project

```text
Server/
├── Program.cs                 # Entry point, DI setup, DB init, service registration
├── Network/
│   ├── NetworkListener.cs     # TcpListener, accept client (port 2003)
│   ├── SessionManager.cs      # Quản lý vòng đời Session, route gửi/nhận
│   ├── PingTracker.cs         # Ping/pong, timeout, đóng session
│   └── s2c_CmdSender.cs       # Xây dựng & gửi s2c command
├── GameLogic/
│   ├── PlayerManager.cs       # IPlayerService, login/signup/logout, player state
│   ├── MatchManager.cs        # Tạo/tìm match, xử lý Execute_Turn, end match
│   ├── Match.cs               # Model 1 trận đấu, board, win-check, validation
│   └── GameValidation.cs      # (tùy chọn) logic validate input/anti-cheat
└── Database/
    ├── SQLiteDbContext.cs     # Tạo và quản lý kết nối SQLite
    └── PlayerRepository.cs    # IPlayerRepository với schema an toàn (hash password)
```

### 4.2. Shared Project

```text
Shared/
├── Network/
│   ├── Packet.cs              # Packet format, PacketReader/Writer
│   ├── Session.cs             # Base session (client & server dùng chung)
│   └── ConstData.cs           # Hằng số (STR_BYTES_MAX_SIZE, timeout, v.v.)
├── GameLogic/
│   ├── c2s_command.cs         # Command C2S struct, tuân theo IByteSerializable
│   ├── s2c_command.cs         # Command S2C struct, tuân theo IByteSerializable
│   └── Player.cs              # Player, PlayerData, EPlayerState
└── Common/
    ├── StructByteConverter.cs # Struct <-> byte[] (cho struct không có mảng động)
    ├── ServiceLocator.cs      # (tạm thời) Service locator, sẽ dần thay bằng DI
    ├── ObjectPool.cs          # Object pooling
    ├── Singleton.cs           # Singleton base (chỉ cho một số global service)
    └── Logger.cs              # Logging đơn giản ra console
```

### 4.3. Client Project

```text
Client/
├── Program.cs                 # Entry point CLI
├── GameSession.cs             # Kế thừa Session, wrapper gửi command C2S
└── GameManager.cs             # Xử lý các gói S2C, hiển thị UI CLI
```

---

## 5. Dependency Injection & Service Wiring

### 5.1. Mục tiêu

- Hạn chế dần ServiceLocator, dùng DI chuẩn của .NET ở cấp `Server`.
- Đảm bảo tất cả service quan trọng đều được đăng ký rõ ràng:
  - `IPlayerRepository`
  - `IPlayerService`
  - `MatchManager`
  - `PingTracker`

### 5.2. Đăng ký Service (ý tưởng)

Trong `Program.cs`:

- Tạo `ServiceCollection`.
- Đăng ký:
  - `IPlayerRepository` → `SQLitePlayerRepository` (scoped hoặc singleton tùy cách dùng).
  - `IPlayerService` → `PlayerManager` (singleton).
  - `MatchManager` (singleton).
  - `PingTracker` (singleton, sử dụng `SessionManager`).
- Inject các service vào handler / manager thay vì gọi `ServiceLocator.GetService<T>()` ở mọi nơi.

Trong giai đoạn chuyển tiếp:

- `ServiceLocator` vẫn có thể tồn tại nhưng:
  - Chỉ như bridge tạm, dùng bên trong `Program.cs` để publish các service DI vào chỗ code cũ.
  - Mục tiêu lâu dài là **loại bỏ**.

---

## 6. Network Architecture

### 6.1. Packet Format

```text
┌─────────────┬─────────────┬─────────────────┐
│  Length     │   Header    │     Payload     │
│  (4 bytes)  │  (4 bytes)  │   (variable)    │
└─────────────┴─────────────┴─────────────────┘
```

- **Length**: Tổng số byte của packet (bao gồm length + header + payload).
- **Header**: Giá trị `EPacketHeader` (uint), xác định loại command.
- **Payload**: Dữ liệu nhị phân cụ thể cho command (struct C2S/S2C).

### 6.2. Packet Headers (ví dụ)

| Header            | Direction | Purpose                                   |
|-------------------|-----------|-------------------------------------------|
| `Disconnect`      | C2S       | Client yêu cầu ngắt kết nối              |
| `Login`           | C2S       | Đăng nhập                                 |
| `SignUp`          | C2S       | Đăng ký                                  |
| `Logout`          | C2S       | Đăng xuất                                |
| `Find_Match`      | C2S       | Tìm trận                                 |
| `Execute_Turn`    | C2S       | Đặt quân tại (x, y)                      |
| `ClientPong`      | C2S       | Phản hồi ping từ client                  |
| `Login_Response`  | S2C       | Kết quả đăng nhập + player id            |
| `SignUp_Response` | S2C       | Kết quả đăng ký                          |
| `Logout_Response` | S2C       | Kết quả đăng xuất                        |
| `Match_Start`     | S2C       | Trận bắt đầu (thông tin match + players) |
| `Start_Turn`      | S2C       | Bắt đầu lượt của 1 player + board state  |
| `Match_End`       | S2C       | Trận kết thúc (thắng/thua/hòa)           |
| `ServerPing`      | S2C       | Ping từ server                           |

### 6.3. Session Lifecycle

```text
Created (pool)
  → Start(sessionId, tcpClient)
  → ReceiveLoopAsync
      → OnPacketReceived (per packet)
      → OnClosed (disconnect)
  → Release to pool
```

- **Session states**:
  - `Connected` → `Authenticated` → `InGame` → `Connected` (sau match) → `Closed`.
- **Player binding**:
  - Sau khi login/signup thành công, `session.BindPlayer(player)` gắn player vào session.

Trong v2, kiến trúc vẫn **gắn chặt session với TCP connection**, nhưng đã:

- Rõ ràng hóa vai trò.
- Chuẩn bị để sau này có thể thêm `AuthToken` và luồng reconnect.

---

## 7. Serialization & Command Structs

### 7.1. Quy ước chung (theo rule Game Server Structs)

- Tất cả command struct (c2s_* / s2c_*) phải:
  - Có `[StructLayout(LayoutKind.Sequential, Pack = 1)]`.
  - Implement `IByteSerializable` (`ToBytes()` + ctor `StructName(byte[] bytes)`).

### 7.2. ToBytes()

- **Struct không có mảng động**:

  - Dùng `StructByteConverter.ToBytes(this)`:

    ```csharp
    public byte[] ToBytes() => StructByteConverter.ToBytes(this);
    ```

- **Struct có mảng động (byte[] / T[])**:
  - Dùng cách build thủ công:
    - Ghi lần lượt các field vào `List<byte>`.
    - Đảm bảo thứ tự đọc/ghi khớp nhau.

### 7.3. Constructor từ byte[]

- Cần parse các field theo đúng thứ tự đã ghi trong `ToBytes()`:
  - Dùng `int offset` để di chuyển con trỏ đọc.
  - Với trường kích thước + mảng (size + byte[]): đọc size, cấp mảng, copy từ `bytes` vào mảng.

### 7.4. byte[] biểu diễn string

- Dùng pattern: `FieldSize` (int) + `FieldBytes` (byte[]).
- Khi decode string, luôn dùng:

  ```csharp
  Encoding.UTF8.GetString(FieldBytes, 0, FieldSize)
  ```

---

## 8. Game Logic & Validation

### 8.1. Player & State Machine

Trạng thái player:

```text
Offline → Online (login) → FindMatch → InMatch → Online (sau match)
```

- `PlayerManager` (implements `IPlayerService`) chịu trách nhiệm:
  - Login/Signup/Logout.
  - Cập nhật `EPlayerState`.
  - Cung cấp API cho `MatchManager`:
    - `GetRandomPlayerByState(EPlayerState.FindMatch, out int foundCount)`.
    - `GetPlayer(int playerId)`.

### 8.2. Match Model

- **Thành phần chính**:
  - `PlayerA`, `PlayerB`.
  - Board: danh sách `Cell` hoặc cấu trúc tối ưu hơn (mảng 2D).
  - Trạng thái lượt: flag boolean hoặc enum xác định player đang được đánh.
  - Tham số N-in-a-row (3 hoặc 5) để hỗ trợ cả Tic-Tac-Toe và Caro.

- **Logic chính**:
  - `HandlePlayerExecutedTurn(Session session, int x, int y)`:
    - Validate (x, y).
    - Nếu hợp lệ:
      - Đánh dấu cell.
      - Check thắng/hòa.
      - Nếu chưa kết thúc, chuyển lượt và gửi `Start_Turn` cho người tiếp theo.

### 8.3. Validation & Anti-cheat (v2 – rõ ràng hơn)

Toàn bộ validation phải nằm **trên server**:

- Check:
  - Player có thuộc match này không.
  - Đúng lượt của player đó không.
  - (x, y) trong biên board.
  - Cell tại (x, y) đang trống.
  - Match chưa kết thúc.

Nếu bất kỳ điều kiện nào sai:

- Bỏ qua request hoặc gửi gói tin báo lỗi (tùy thiết kế).
- Có thể log lại để phân tích hành vi bất thường.

---

## 9. Matchmaking Flow (v2)

Flow cơ bản (giữ giống v1 nhưng rõ ràng hơn):

1. Player A gửi `Find_Match`.
2. `PlayerManager` → set state của A = `FindMatch`.
3. `MatchManager` gọi `IPlayerService.GetRandomPlayerByState(FindMatch)` để tìm B.
4. Nếu tìm được B:
   - Tạo `Match` mới với A, B.
   - Đặt state A, B = `InMatch`.
   - Gửi `Match_Start` tới cả A và B.
   - Gửi `Start_Turn` cho player đi trước.
5. Nếu không tìm được:
   - Player A tiếp tục ở trạng thái `FindMatch` (chờ).

---

## 10. Authentication & Password Security

### 10.1. Schema SQLite (v2)

```sql
CREATE TABLE Players (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Username TEXT NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    Salt TEXT NOT NULL,
    CreatedDate TEXT NOT NULL,
    LastLoginDate TEXT NOT NULL
);
```

- **Thay đổi chính**:
  - `Password` → `PasswordHash` + `Salt`.

### 10.2. Luồng SignUp / Login (mức khái quát)

- **SignUp**:
  - Nhận username, password thô từ client.
  - Tạo salt ngẫu nhiên, hash password (Password + Salt) bằng thuật toán an toàn.
  - Lưu `Username`, `PasswordHash`, `Salt` vào DB.

- **Login**:
  - Đọc user từ DB (theo Username).
  - Lấy Salt, băm lại password client gửi lên.
  - So sánh với `PasswordHash`.
  - Nếu ok:
    - Đánh dấu player `Online`.
    - Bind vào session hiện tại.

---

## 11. Connection Health, Ping/Pong & Disconnect

- `PingTracker` chạy vòng lặp định kỳ:
  - Với mỗi session:
    - Nếu thời gian từ lần gửi ping cuối > `PingCooldown`:
      - Gửi `ServerPing` (kèm timestamp).
    - Nếu thời gian từ lần nhận pong cuối > `PongDelayThreshold`:
      - Đóng session (`Session.Close()`).

- Khi session đóng:
  - `SessionManager.HandleSessionClosed`:
    - Gỡ session khỏi pool.
    - Thông báo tới `MatchManager` để xử lý:
      - Nếu player đang trong match:
        - Kết thúc match, player còn lại thắng do đối thủ disconnect.

---

## 12. Fit with Online Game Architecture & Future Directions

### 12.1. Mức độ phù hợp hiện tại

- Phù hợp cho:
  - Game 1vs1, số lượng người chơi nhỏ – trung bình.
  - Demo / prototype.
  - Môi trường lab / học tập.

- Các điểm tích cực:
  - **Server-authoritative** cho logic match.
  - Chia module rõ ràng (Network, GameLogic, Database, Shared).
  - Có health check (ping/pong).
  - Matchmaking đơn giản nhưng đủ dùng cho 1vs1.

### 12.2. Hạn chế & hướng mở rộng multi-server

Hạn chế lớn (không giải quyết trong v2, chỉ định hướng):

- Single process, không multi-server.
- SQLite giới hạn scale.
- Chưa có gateway server, chưa có session token phục vụ reconnect across server.

Hướng v3+:

- Thêm:
  - **Gateway server** (nhận kết nối TCP, định tuyến request).
  - **Match server** (xử lý logic trận đấu).
  - DB server (Postgres/MySQL) + cache (Redis).
  - Session token và luồng reconnect match.

---

## 13. Summary

- `ARCHITECTURE_V2` mô tả kiến trúc **mục tiêu** đã cải thiện so với bản đầu:
  - Sửa **bảo mật mật khẩu** (hash + salt).
  - Chuẩn hóa `Shared` thành **Class Library**.
  - Đảm bảo `IPlayerService` được đăng ký và dùng qua DI.
  - Rõ ràng hơn về validation/anti-cheat trong xử lý lượt chơi.
- Kiến trúc này vẫn là **single-node TCP game server**, phù hợp cho game Caro/Gomoku nhỏ, và là nền tảng để mở rộng lên kiến trúc multi-server về sau.

