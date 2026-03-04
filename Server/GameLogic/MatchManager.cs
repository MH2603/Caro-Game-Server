using Server.Network;
using Shared.GameLogic;
using Shared.Logic;
using Shared.Network;
using System.Collections.Concurrent;

namespace Server.GameLogic
{
    public class MatchManager : IPacketHandler, IDisposable
    {
        private ConcurrentDictionary<int, Match> _matchMap = new ();
        int _matchIndexCounter = 1;

        // other dependents
        IPlayerService playerService;

        public MatchManager(IPlayerService playerService)
        {
            // hanler cmd from Clients
            PacketDispatcher.Instance.RegisterHandler(EPacketHeader.Find_Match, this);
            PacketDispatcher.Instance.RegisterHandler(EPacketHeader.Execute_Turn, this);

            SessionManager.Instance.RegisterSessionClosedCallback(HandleSessionClosed);

            this.playerService = playerService;
        }

        void IDisposable.Dispose()
        {
            PacketDispatcher.Instance.UnregisterHandler(EPacketHeader.Find_Match);
            PacketDispatcher.Instance.UnregisterHandler(EPacketHeader.Execute_Turn);

            SessionManager.Instance.UnregisterSessionClosedCallback(HandleSessionClosed);
        }

        #region Event Callbacks

        public void HandlePacket(Session session, Packet packet)
        {
            switch (packet.Header)
            {
                case EPacketHeader.Find_Match:
                    HandleFindMatchRequire(session);
                    break;
                case EPacketHeader.Execute_Turn:
                    HandlePlayerExecutedTurn(session.Player.Id, packet);
                    break;
            }
        }

        private void HandlePlayerExecutedTurn(int senderId, Packet packet)
        {
            c2s_execute_turn cmd = new c2s_execute_turn(packet.Data);

            if ( _matchMap.ContainsKey(cmd.MatchId))
            {
                _matchMap[cmd.MatchId].HandlePlayerExecutedTurn(senderId, cmd);
            }
            else
            {
                Logger.LogError($" Could find any match with id={cmd.MatchId}");
            }
        }

        private void HandleFindMatchRequire(Session session)
        {
            if (session.Player.State != EPlayerState.Online)
            {
                Logger.Log($" CLIENT:  {session.Player.Data.Username} FIND_MATCH but state = {session.Player.State} ");
                return;
            }

            session.Player.ChangeState(EPlayerState.FindMatch);
            Logger.Log($" Player {session.Player.Data.Username} is finding a match ... ");

            // case 1: find a other player
            var player_B = playerService.GetRandomPlayerByState(EPlayerState.FindMatch, out var foundCount, session.Player.Id);
            if (foundCount > 0)
            {
                CreateNewMatch(session.Player, player_B);
                Logger.Log($" A match started between {session.Player.Data.Username} with {player_B.Data.Username}");
                return;
            }

        }

        private void HandleMatchEnd(Match match)
        {
            if (match == null) return;

            // 1. Sử dụng biến cục bộ riêng để hứng đối tượng lấy ra từ Map
            if (_matchMap.TryRemove(match.Id, out var removedMatch))
            {
                // 2. Chỉ Dispose nếu chúng ta thực sự là người lấy nó ra khỏi Map thành công
                // Đảm bảo đối tượng lấy ra chính là đối tượng ta muốn (Optionally)
                removedMatch.Dispose();
            }
            else
            {
                // 3. Log hoặc xử lý nếu Match này vốn dĩ đã bị xóa trước đó bởi luồng khác
                // Logger.LogWarning($"Match {match.Id} already removed.");
            }
        }

        private void HandleSessionClosed(Session session)
        {
            if(session.Player == null || session.Player.State != EPlayerState.InMatch)
            {
                return;
            }

            var playerId = session.Player.Id;
            foreach (var match in _matchMap.Values)
            {
                match.HandleSessionClosed(playerId);
            }
        }

        #endregion

        void CreateNewMatch(Player playerA, Player playerB)
        {
            var match = new Match(playerA, playerB, _matchIndexCounter);
            _matchMap.TryAdd(match.Id, match);
            match.OnMatchEnd += HandleMatchEnd;

            _matchIndexCounter++;
        }
    }
}
