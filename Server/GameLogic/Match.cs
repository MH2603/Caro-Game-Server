using Server.Network;
using Shared.GameLogic;
using Shared.Logic;
using Shared.Network;
using System.Numerics;

namespace Server.GameLogic
{
    public class Match
    {
        Player A; //  player A
        Player B; //  player B

        bool _isTurnOfA;
        List<Cell> _cells= new List<Cell>();

        const int WinConditionCombo = 3;

        public Action<Match> OnMatchEnd;

        public Match(Player playerA, Player playerB) 
        { 
            A = playerA;
            B = playerB;

            A.ChangeState(EPlayerState.InMatch);
            B.ChangeState(EPlayerState.InMatch);

            _isTurnOfA = true;
            _cells = new List<Cell>();

            CmdSender.SendTurnStartCmd( _isTurnOfA ? A.Id : B.Id, _cells.ToArray());
        }  

        public void HandlePlayerExecutedTurn( int playerId ,c2s_execute_turn cmd)
        {
            // check legit of this execute
            if(!CheckLegitOfExecuteTurnCmd(playerId, cmd)) return;

            MarkCell(cmd.x, cmd.y);

            CmdSender.SendTurnStartCmd(_isTurnOfA ? A.Id : B.Id, _cells.ToArray());

            CheckToEndMatch(cmd.x, cmd.y);

            _isTurnOfA = !_isTurnOfA;
        }

        void CheckToEndMatch(int x, int y)
        {
            int markType = _isTurnOfA ? 1 : 2;

            int hori_combo = CountCombo(x, y, 1, 0, WinConditionCombo, markType);
            int ver_combo  = CountCombo(x, y, 0, 1, WinConditionCombo, markType);
            int diagonal_up_combo = CountCombo(x, y, 1, 1, WinConditionCombo, markType);
            int diagonal_down_combo = CountCombo(x, y, 1, -1, WinConditionCombo, markType);

            if (hori_combo >= WinConditionCombo ||
                ver_combo >= WinConditionCombo ||
                diagonal_up_combo >= WinConditionCombo ||
                diagonal_down_combo >= WinConditionCombo)
            {
                int winnerId = _isTurnOfA ? A.Id : B.Id;
                CmdSender.SendMatchEnd(A.Id, false, winnerId);
                CmdSender.SendMatchEnd(B.Id, false, winnerId);

                A.ChangeState(EPlayerState.Online);
                B.ChangeState(EPlayerState.Online);

                OnMatchEnd?.Invoke(this);
            }
                
        }

        int CountCombo( int pos_x, int pos_y,int dir_x, int dir_y, int range, int markValue)
        {
            pos_x -= dir_x * range;
            pos_y -= dir_y * range;
            int combo = 0;
            for (int i=0; i < range * 2; i ++)
            {
                pos_x += dir_x;
                pos_y += dir_y;

                if ( GetCell(pos_x, pos_y).Status == markValue)
                {
                    combo++;
                }
            }

            return combo;
        }

        bool CheckLegitOfExecuteTurnCmd(int playerId, c2s_execute_turn cmd)
        {
            if (_isTurnOfA && playerId != A.Id)
            {
                return false;   
            }

            if (!_isTurnOfA && playerId != B.Id) 
            { 
                return false;    
            }

            var cell = GetCell(cmd.x, cmd.y);
            if(cell.Status != 0 ) return false;

            return true;
        }

        Cell GetCell( int x, int y)
        {
            for ( int i=0; i < _cells.Count; i++)
            {
                if (_cells[i].Pos.X == x && _cells[i].Pos.Y == y)
                {
                    return _cells[i];   
                }
            }

            return default;
        }

        void MarkCell(int x, int y)
        {
            Cell cell = GetCell(x, y);
            cell.Status = _isTurnOfA ? 1 : 2;   
        }

    }

    public class MatchManager : IPacketHandler
    {
        private List<Match> _matchs = new List<Match>();

        public MatchManager() 
        {
            PacketDispatcher.Instance.RegisterHandler(EPacketHeader.Find_Match, this);
        }

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
            c2s_execute_turn cmd = StructByteConverter.ToStruct<c2s_execute_turn>(packet.Data);

            for (int i=0; i < _matchs.Count; i ++)
            {
                _matchs[i].HandlePlayerExecutedTurn(senderId, cmd);
            }
        }

        private void HandleFindMatchRequire(Session session)
        {
            session.Player.ChangeState(EPlayerState.FindMatch);

            // case 1: find a other player
            var player_B = ServiceLocator.GetService<IPlayerService>().GetRandomPlayerByState(EPlayerState.FindMatch, out var foundCount);
            if (foundCount > 0)
            {
                CreateNewMatch(session.Player, player_B);
                return;
            }
            
        }

        void CreateNewMatch( Player playerA, Player playerB)
        {
            var match = new Match(playerA, playerB);
            _matchs.Add(match);

            CmdSender.SendMatchStartCmd(playerA.Data.Id, playerB.Data.Id);

            match.OnMatchEnd += HandleMatchEnd;
        }

        private void HandleMatchEnd(Match match)
        {
            if (match != null)
            {
                match.OnMatchEnd -= HandleMatchEnd;
                _matchs.Remove(match);
            }

            
        }
    }
}
