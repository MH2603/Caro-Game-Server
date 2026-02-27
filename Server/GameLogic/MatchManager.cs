using Server.Network;
using Shared.GameLogic;
using Shared.Logic;
using Shared.Network;
using System.Collections.Concurrent;

namespace Server.GameLogic
{
    public class MatchManager : IPacketHandler, IDisposable
    {
        private ConcurrentDictionary<int, Match> _matchs = new ();
        int _matchIndexCounter = 1;

        // other dependents
        IPlayerService playerService;

        public MatchManager(IPlayerService playerService)
        {
            PacketDispatcher.Instance.RegisterHandler(EPacketHeader.Find_Match, this);

            this.playerService = playerService;
        }

        void IDisposable.Dispose()
        {
            PacketDispatcher.Instance.UnregisterHandler(EPacketHeader.Find_Match);
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

            for (int i = 0; i < _matchs.Count; i++)
            {
                _matchs[i].HandlePlayerExecutedTurn(senderId, cmd);
            }
        }

        private void HandleFindMatchRequire(Session session)
        {
            session.Player.ChangeState(EPlayerState.FindMatch);

            // case 1: find a other player
            var player_B = playerService.GetRandomPlayerByState(EPlayerState.FindMatch, out var foundCount, session.Player.Id);
            if (foundCount > 0)
            {
                CreateNewMatch(session.Player, player_B);
                return;
            }

        }

        private void HandleMatchEnd(Match match)
        {
            if (match != null)
            {
                match.OnMatchEnd -= HandleMatchEnd;
                _matchs.TryRemove( match.Id, out match);
            }


        }

        #endregion

        void CreateNewMatch(Player playerA, Player playerB)
        {
            var match = new Match(playerA, playerB, _matchIndexCounter );
            _matchs.TryAdd(match.Id, match);

            CmdSender.SendMatchStartCmd(playerA.Data.Id, playerB.Data.Id);

            match.OnMatchEnd += HandleMatchEnd;

            _matchIndexCounter++;
        }

        
    }
}
