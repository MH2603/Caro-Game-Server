# ServerForUnity – Architecture Document

**Purpose:** Explain the architecture of the Caro Chess (Gomoku) game server and test client.  
**Audience:** Developers who maintain, debug, or extend the `ServerForUnity` solution.  
**Tech Stack:** .NET 8.0, TCP sockets, SQLite.

---

## 1. Overview

ServerForUnity is a TCP-based game server that provides:

- **Authentication** – Login and sign-up
- **Matchmaking** – Find opponent and start match
- **Game logic** – Turn-based board game (currently 3-in-a-row, extensible to Caro 5-in-a-row)
- **Session management** – Connection handling, packet routing, disconnect handling
- **Connection health** – Periodic server ping and client pong to detect dead sessions

The architecture follows a **handler/dispatcher** pattern: incoming packets are routed by header to registered handlers. Shared code (network, commands, game models) lives in the `Shared` project; server and client each reference it.

---

## 2. Solution Structure

```
ServerForUnity/
├── Server/              # Game server (console app)
├── Client/               # Test client (console app)
├── Shared/               # Shared code (network, commands, models)
├── Test/                 # Test project
├── Common/               # Orphaned (not in solution)
└── Server For Unity.sln
```

### Project Dependencies

```
                    ┌─────────┐
                    │  Test   │
                    └────┬────┘
                         │
            ┌────────────┼────────────┐
            ▼            ▼            ▼
       ┌─────────┐  ┌─────────┐  ┌─────────┐
       │ Client  │  │ Server  │  │ Shared  │
       └────┬────┘  └────┬────┘  └────┬────┘
            │            │            │
            └────────────┴────────────┘
                         │
                    references
                         │
                         ▼
                   ┌──────────┐
                   │  Shared  │
                   └──────────┘
```

| Project | Type | Dependencies |
|---------|------|--------------|
| **Server** | Console App | Shared |
| **Client** | Console App | Shared |
| **Shared** | Library* | None |
| **Test** | Test Project | Client, Server, Shared |

*Shared is configured as Exe but used as a library.

---

## 3. Folder Structure & Key Files

```
Server/
├── Program.cs                 # Entry point, DB init, service registration
├── Network/
│   ├── NetworkListener.cs     # TCP listener (port 2003)
│   ├── SessionManager.cs      # Session pool, packet routing
│   ├── PingTracker.cs         # Periodic ping/pong tracking and timeout
│   └── s2c_CmdSender.cs       # Server→client packet builders
├── GameLogic/
│   ├── PlayerManager.cs       # Login, SignUp, IPlayerService
│   ├── Match.cs               # Match + MatchManager
│   ├── AccountService.cs      # Stub (unused)
│   └── IPlayerRepository.cs   # Repository interface
└── Database/
    ├── SQLiteDBContext.cs     # SQLite connection
    └── PlayerRepository.cs    # Player CRUD

Client/
├── Program.cs                 # CLI entry point
├── GameSession.cs             # Session + send commands
└── GameManager.cs             # Packet handlers, UI flow

Shared/
├── Network/
│   ├── Packet.cs              # Packet, PacketReader, PacketDispatcher
│   ├── Session.cs             # TCP session, send/receive
│   └── ConstData.cs           # Constants (e.g. STR_BYTES_MAX_SIZE)
├── GameLogic/
│   ├── c2s_command.cs        # Client→server command structs
│   ├── s2c_command.cs        # Server→client command structs
│   └── Player.cs              # Player, PlayerData, EPlayerState
└── Common/
    ├── StructByteConverter.cs # Struct ↔ byte[] serialization
    ├── ServiceLocator.cs      # DI container
    ├── ObjectPool.cs          # Object pooling
    ├── Singleton.cs           # Singleton base
    └── Logger.cs              # Console logging
```

---

## 4. Component Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              SERVER                                               │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                   │
│  ┌──────────────────┐         ┌──────────────────┐         ┌──────────────────┐ │
│  │ NetworkListener  │ Accept  │  SessionManager  │  Pool   │     Session       │ │
│  │ (TcpListener     │────────►│  (Singleton)     │◄───────│  (per client)     │ │
│  │  port 2003)      │         │                  │         │                   │ │
│  └──────────────────┘         │ - Session pool   │         │ - TcpClient       │ │
│                               │ - running map    │         │ - Player ref       │ │
│                               └────────┬─────────┘         │ - OnPacketReceived│ │
│                                        │                   └─────────┬────────┘ │
│                                        │ Dispatch                     │          │
│                                        ▼                             │          │
│  ┌──────────────────────────────────────────────────────────────────┴──────────┐ │
│  │                        PacketDispatcher (Singleton)                         │ │
│  │              Dictionary<EPacketHeader, IPacketHandler>                       │ │
│  └──────────────────────────────────────┬──────────────────────────────────────┘ │
│                                         │                                         │
│         ┌───────────────────────────────┼───────────────────────────────┐         │
│         ▼                               ▼                               ▼         │
│  ┌──────────────┐              ┌──────────────┐              ┌──────────────┐     │
│  │PlayerManager │              │ MatchManager │              │AccountService│     │
│  │ Login        │              │ Find_Match   │              │ (stub)       │     │
│  │ SignUp       │              │ Execute_Turn │              └──────────────┘     │
│  │ IPlayerSvc   │              └──────┬───────┘                                   │
│  └──────┬───────┘                     │                                           │
│         │                             ▼                                           │
│         ▼                      ┌──────────────┐                                    │
│  ┌──────────────┐              │    Match     │                                    │
│  │IPlayerRepo   │              │ - Player A,B │                                    │
│  │ (SQLite)     │              │ - _cells     │                                    │
│  └──────────────┘              │ - win check  │                                    │
│                                └──────────────┘                                    │
│                                                                                   │
│  ┌──────────────────┐                                                             │
│  │   CmdSender      │  Builds s2c packets, sends via SessionManager.SendPacket     │
│  └──────────────────┘                                                             │
└─────────────────────────────────────────────────────────────────────────────────┘
```

---

## 5. Data Flow

### 5.1 Connection & Packet Flow

```
Client                    Server
  │                         │
  │──── TCP Connect ───────►│  NetworkListener.AcceptTcpClientAsync
  │                         │  SessionManager.StartSession
  │                         │  Session.Start → ReceiveLoopAsync
  │                         │
  │◄─── (idle) ─────────────│  PacketReader.ReadAsync (waiting)
  │                         │
  │──── Send Packet ────────►│  stream.ReadAsync
  │                         │  PacketReader parses [Length][Header][Payload]
  │                         │  OnPacketReceived(session, packet)
  │                         │  SessionManager.HandleReceivedPacket
  │                         │  PacketDispatcher.Dispatch(header)
  │                         │  → Handler.HandlePacket(session, packet)
  │                         │
  │◄─── Response ───────────│  CmdSender.SendXxx → SessionManager.SendPacket
  │                         │  session.SendPacket
```

### 5.2 Authentication Flow

```
Client                          Server
  │                               │
  │  SendLoginCmd(user, pw)       │
  │─────────────────────────────►│  PlayerManager.HandleLogin
  │                               │  IPlayerRepository lookup
  │                               │  session.BindPlayer(player)
  │                               │  CmdSender.SendLoginResponse
  │  Login_Response               │
  │◄─────────────────────────────│
```

### 5.3 Matchmaking Flow

```
Client A              Client B              Server
  │                      │                     │
  │  Find_Match          │                     │
  │──────────────────────────────────────────►│  MatchManager.HandleFindMatch
  │                      │                     │  player.ChangeState(FindMatch)
  │                      │  Find_Match         │  IPlayerService.GetRandomPlayerByState
  │                      │────────────────────►│  CreateNewMatch(A, B)
  │                      │                     │  CmdSender.SendMatchStartCmd
  │  Match_Start         │  Match_Start        │
  │◄──────────────────────────────────────────│
  │                      │◄────────────────────│
```

### 5.4 Match Flow

```
Client (turn)                    Server
  │                                │
  │  Execute_Turn(x, y)            │
  │───────────────────────────────►│  MatchManager.HandlePlayerExecutedTurn
  │                                │  Match.HandlePlayerExecutedTurn
  │                                │  CheckLegit → MarkCell → CheckToEndMatch
  │                                │  CmdSender.SendTurnStartCmd (next) or SendMatchEnd
  │  Start_Turn / Match_End        │
  │◄──────────────────────────────│
```

### 5.5 Ping / Disconnect Flow

```
Client                          Server
  │                               │
  │  (idle)                       │
  │                               │  PingTracker.TickLoop
  │                               │  ├─ For each active Session
  │                               │  │   ├─ If ElapsedSendPingTime > PingCooldown → Send ServerPing
  │                               │  │   └─ If ElapsedReceivePongTime > PongDelayThreshold → Session.Close()
  │                               │  └─ SessionManager.HandleSessionClosed
  │                               │
  │◄────────── s2c_ping ──────────│  Session.SendCmd(ServerPing, s2c_ping)
  │                               │
  │────────── c2s_pong ──────────►│  PacketDispatcher → PingTracker.HandlePacket
  │                               │  Reset ElapsedReceivePongTime
  │                               │
  │           (no pong)           │
  │           ────────────────x   │  Session.Close() → Match.HandleSessionClosed(winner by disconnect)
```

---

## 6. Network Layer

### 6.1 Packet Format

```
┌─────────────┬─────────────┬─────────────────┐
│  Length     │   Header    │     Payload     │
│  (4 bytes)  │  (4 bytes)  │   (variable)    │
└─────────────┴─────────────┴─────────────────┘
```

- **Length:** Total packet size (including length and header)
- **Header:** `EPacketHeader` enum (uint)
- **Payload:** Command-specific binary data (struct serialized via `StructByteConverter`)

### 6.2 Packet Headers

| Header | Direction | Purpose |
|--------|-----------|---------|
| `Disconnect` | C2S | Client-initiated disconnect |
| `Login` | C2S | Login request |
| `SignUp` | C2S | Sign-up request |
| `Logout` | C2S | Logout request |
| `Find_Match` | C2S | Request matchmaking |
| `Execute_Turn` | C2S | Place piece (x, y) in a match |
| `ClientPong` | C2S | Pong response with timestamp |
| `Login_Response` | S2C | Login result + player id |
| `SignUp_Response` | S2C | Sign-up result |
| `Logout_Response` | S2C | Logout result |
| `Match_Start` | S2C | Match started (match id + players) |
| `Start_Turn` | S2C | Turn start + board state (cells) |
| `Match_End` | S2C | Match over (winner/draw) |
| `ServerPing` | S2C | Ping with timestamp |

### 6.3 Session Lifecycle

```
Created (pool) → Start(sessionId, tcpClient) → ReceiveLoopAsync
    → OnPacketReceived (per packet)
    → OnClosed (disconnect) → Release to pool
```

**Session states:** `Connected` → `Authenticated` (after login) → `InGame` (in match)

**Player binding:** `session.BindPlayer(player)` after successful login/signup.

### 6.4 Serialization

- **StructByteConverter:** Reflection-based conversion for structs with `[StructLayout(LayoutKind.Sequential, Pack=1)]`
- **Command structs:** Use `MarshalAs` for fixed-size arrays (e.g. `STR_BYTES_MAX_SIZE`)

---

## 7. Game Logic

### 7.1 Player States

```
Offline → Online (login) → FindMatch → InMatch → Online (match end)
```

### 7.2 Match Model

- **Players:** Player A, Player B
- **Board:** `List<Cell>` (each cell: `Pos` (Vector2), `Status` 0/1/2)
- **Turn:** `_isTurnOfA` toggles each move
- **Win:** `CountCombo` checks horizontal, vertical, diagonals for N-in-a-row (N=3 or 5)

### 7.3 Service Dependencies

| Service | Implemented By | Registered |
|---------|----------------|------------|
| `IPlayerRepository` | SQLitePlayerRepository | ✓ Program.cs |
| `IPlayerService` | PlayerManager | ✗ (missing) |

---

## 8. Database Layer

**Technology:** SQLite via `Microsoft.Data.Sqlite.Core`

**Schema (Players):**
```sql
CREATE TABLE Players (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Username TEXT NOT NULL UNIQUE,
    Password TEXT NOT NULL,
    CreatedDate TEXT NOT NULL,
    LastLoginDate TEXT NOT NULL
);
```

**Components:**
- `SQLiteDbContext` – Connection factory
- `IPlayerRepository` – `AddAsync`, `UpdateAsync`, `GetAll`
- `SQLitePlayerRepository` – Implementation

---

## 9. Design Patterns

| Pattern | Usage |
|---------|-------|
| **Singleton** | `PacketDispatcher`, `SessionManager` via `Singleton<T>` |
| **Service Locator** | `ServiceLocator.RegisterService<T>` / `GetService<T>` |
| **Object Pool** | `ObjectPool<Session>` for session reuse |
| **Handler/Dispatcher** | `IPacketHandler` + `PacketDispatcher` for packet routing |
| **Repository** | `IPlayerRepository` for data access |

---

## 10. Key Interfaces

```csharp
// Packet handler – registered per EPacketHeader
interface IPacketHandler {
    void HandlePacket(Session session, Packet packet);
}

// Player data access
interface IPlayerRepository {
    Task AddAsync(PlayerData data);
    Task UpdateAsync(PlayerData data);
    Task<PlayerData[]> GetAll();
}

// Player lookup for matchmaking
interface IPlayerService {
    Player GetRandomPlayerByState(EPlayerState state, out int foundCount);
    Player GetPlayer(int playerId);
}
```

---

## 11. Client Architecture

```
Program.cs (CLI)
    │
    ├── GameSession (extends Session)
    │       └── SendLoginCmd, SendSignUpCmd, SendFindMatchCmd, SendExecuteTurnCmd
    │
    └── GameManager
            └── Handles: Login_Response, Match_Start (partial)
```

The client uses the same `Session` base and `Packet` format. It connects to the server, sends commands, and handles responses in `GameManager`.

---

*Document reflects current project state. See `DEVELOPMENT_PLAN.md` for known issues and planned fixes.*
