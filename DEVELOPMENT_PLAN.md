# Caro Chess Server – 1-Week Development Plan

**Timeline:** 7 days × 2 hours/day = **14 hours total**  
**Goal:** Simple Caro chess server with sign-in, matchmaking, and win/lose flow  
**Task size:** ~2 hours per task

---

## Current Project Status (Summary)

| Area | Status | Notes |
|------|--------|------|
| **Auth** | Partial | Login/SignUp exist; plain-text passwords; some bugs |
| **Matchmaking** | Partial | Find match exists; `IPlayerService` not registered |
| **Game logic** | Broken | Board never initialized; `MarkCell` updates copy, not list |
| **Network** | Partial | TCP works; `PacketReader` bug; client sends wrong commands |
| **Client** | Partial | CLI exists; `SendFindMatchCmd`/`SendExecuteTurnCmd` wrong |

---

## Week Overview

| Day | Focus | Duration |
|-----|-------|----------|
| 1 | Fix board & game logic | 2h |
| 2 | Fix DI, registration & matchmaking | 2h |
| 3 | Fix network & client commands | 2h |
| 4 | Matchmaking queue & match end flow | 2h |
| 5 | Upgrade to Caro (5-in-a-row, 15×15) | 2h |
| 6 | Client flow & end-to-end testing | 2h |
| 7 | Polish, error handling & docs | 2h |

---

## Day 1: Fix Board & Core Game Logic (2h)

**Goal:** Board works correctly and moves are applied.

### Tasks
1. **Initialize board in `Match`**
   - Create 3×3 grid of `Cell` (or 15×15 for Caro later)
   - Populate `_cells` in constructor with `Status = 0`

2. **Fix `MarkCell`**
   - Update the cell in `_cells`, not a local copy
   - Use index-based update or replace the cell in the list

3. **Fix `GetCell`**
   - Add bounds check for `x`, `y`
   - Return default for out-of-bounds

4. **Fix `CountCombo`**
   - Add bounds checking so it does not read outside the board

**Files:** `Server/GameLogic/Match.cs`

**Done when:** A move is placed and win detection works for 3-in-a-row.

---

## Day 2: Fix DI & Matchmaking Registration (2h)

**Goal:** Matchmaking can find another player.

### Tasks
1. **Register `IPlayerService`**
   - In `Program.cs`: `ServiceLocator.RegisterService<IPlayerService>(playerManager);`
   - Ensure `playerManager` is created before `MatchManager` uses it

2. **Session–Player mapping**
   - Add `Dictionary<int, Session>` in `SessionManager` for lookup by player ID
   - Update on login and disconnect

3. **Fix `SendMatchStartCmd`**
   - Ensure both players receive match start with correct IDs
   - Verify `CmdSender` uses sessions correctly

4. **Fix `HandleSignUp` fault path**
   - Do not use `session.Player.Id` when signup fails (player may not exist)
   - Send fault via session only

**Files:** `Server/Program.cs`, `Server/Network/SessionManager.cs`, `Server/GameLogic/PlayerManager.cs`, `Server/Network/s2c_CmdSender.cs`

**Done when:** Two clients can find each other and start a match.

---

## Day 3: Fix Network & Client Commands (2h)

**Goal:** Client sends correct packets; server parses them correctly.

### Tasks
1. **Fix `PacketReader`**
   - Use `_cacheBuffer` instead of `bytes` when parsing length/header

2. **Fix `Client/GameSession.cs`**
   - `SendFindMatchCmd`: use `c2s_FindMatch()` instead of `c2s_signup()`
   - `SendExecuteTurnCmd`: pass `(x, y)` to `c2s_execute_turn(x, y)`

3. **Implement `c2s_login` serialization**
   - Add `ToByte()` and `FromByte()` for `c2s_login` if used

4. **Verify `StructByteConverter`**
   - Ensure `c2s_execute_turn` and `c2s_FindMatch` serialize/deserialize correctly

**Files:** `Shared/Network/Packet.cs`, `Client/GameSession.cs`, `Shared/GameLogic/c2s_command.cs`, `Shared/Common/StructByteConverter.cs`

**Done when:** Client can login, find match, and send moves; server receives them correctly.

---

## Day 4: Matchmaking Queue & Match End Flow (2h)

**Goal:** Players wait in queue; match end is handled cleanly.

### Tasks
1. **Queue behavior**
   - When no opponent: keep player in `FindMatch` state
   - Optionally send “waiting for opponent” response

2. **Match end**
   - Ensure both players receive `Match_End` with winner/loser
   - Remove both from match and return to `Online` state

3. **Draw handling**
   - Detect full board with no winner
   - Send draw result to both players

4. **Disconnect handling**
   - If a player disconnects mid-match, end match and notify opponent

**Files:** `Server/GameLogic/Match.cs`, `Server/GameLogic/MatchManager.cs`, `Shared/GameLogic/s2c_command.cs`

**Done when:** Queue works, match end and draw are handled, disconnect is handled.

---

## Day 5: Upgrade to Caro (5-in-a-Row, 15×15) (2h)

**Goal:** Game rules match Caro (Gomoku).

### Tasks
1. **Board size**
   - Change board to 15×15 (or 19×19)
   - Update `Match` constructor to create full grid

2. **Win condition**
   - Set `WinConditionCombo = 5`
   - Ensure `CountCombo` supports 5-in-a-row and bounds

3. **Protocol**
   - Confirm `Cell`/board data fits packet size
   - Adjust if needed for larger board

4. **Client**
   - Update display for 15×15 board (if CLI shows board)

**Files:** `Server/GameLogic/Match.cs`, `Shared/GameLogic/` (if structs change), `Client/GameManager.cs`

**Done when:** Game is 5-in-a-row on 15×15 board.

---

## Day 6: Client Flow & End-to-End Testing (2h)

**Goal:** Full flow works from login to match end.

### Tasks
1. **Client handlers**
   - Handle `SignUp_Response`
   - Handle `Match_End` (winner/loser/draw)
   - Handle `Start_Turn` (board state)

2. **CLI flow**
   - Login → Find Match → Play turns → See result
   - Add `turn_execute` to `ShowCmd` if missing

3. **End-to-end test**
   - Two clients: signup, login, find match, play until win
   - Verify win/lose/draw and state transitions

4. **Edge cases**
   - Invalid move (occupied cell, out of bounds)
   - Wrong turn

**Files:** `Client/GameManager.cs`, `Client/Program.cs`, `Client/GameSession.cs`

**Done when:** Two clients can complete a full match with correct results.

---

## Day 7: Polish, Error Handling & Documentation (2h)

**Goal:** Stable, understandable, and documented.

### Tasks
1. **Error handling**
   - Graceful handling of malformed packets
   - Clear error messages for invalid login/signup

2. **Password security**
   - Hash passwords (e.g. BCrypt) before storing
   - Update login to verify hash

3. **Logging**
   - Add logs for key events (login, match start, match end)
   - Avoid logging sensitive data

4. **Documentation**
   - README: how to run server and client
   - Protocol overview (packet headers, basic flow)
   - Known limitations

**Files:** `Server/`, `Shared/`, `README.md`

**Done when:** Server is stable, secure enough for demo, and documented.

---

## Quick Reference: Critical Bugs to Fix

| # | Bug | Location | Fix |
|---|-----|----------|-----|
| 1 | Board never initialized | `Match.cs` | Populate `_cells` in constructor |
| 2 | `MarkCell` updates copy | `Match.cs` | Update list element by index |
| 3 | `IPlayerService` not registered | `Program.cs` | `RegisterService<IPlayerService>(playerManager)` |
| 4 | `SendFindMatchCmd` uses signup | `GameSession.cs` | Use `c2s_FindMatch()` |
| 5 | `SendExecuteTurnCmd` no (x,y) | `GameSession.cs` | Pass `(x, y)` to constructor |
| 6 | `PacketReader` uses wrong buffer | `Packet.cs` | Use `_cacheBuffer` for parsing |
| 7 | SignUp fault uses null player | `PlayerManager.cs` | Send via session only |

---

## Optional (If Time Allows)

- Session lookup by player ID
- AccountService integration or removal
- Shared project as class library
- Unit tests for `Match` logic

---

*Plan created for ServerForUnity Caro Chess project. Adjust task order if dependencies require it.*
