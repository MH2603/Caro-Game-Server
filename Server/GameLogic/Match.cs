using Server.Network;
using Shared.GameLogic;
using Shared.Logic;
using Shared.Network;
using System.Numerics;

namespace Server.GameLogic
{
    public class Match
    {
        #region FIELDS
        int _id;

        Player A; //  player A
        Player B; //  player B

        bool _isTurnOfA;
        List<Cell> _cells = new List<Cell>();

        const int WinConditionCombo = 3;
        #endregion


        #region PROPERTIES
        public Action<Match> OnMatchEnd;
        public int Id => _id;

        #endregion


        public Match(Player playerA, Player playerB, int index=0) 
        { 
            A = playerA;
            B = playerB;

            A.ChangeState(EPlayerState.InMatch);
            B.ChangeState(EPlayerState.InMatch);

            _isTurnOfA = true;
            _cells = new List<Cell>();

            int nextTurnPlayerId = _isTurnOfA ? A.Id : B.Id;
            CmdSender.SendTurnStartCmd( A.Id, nextTurnPlayerId, _cells.ToArray());
            CmdSender.SendTurnStartCmd( B.Id, nextTurnPlayerId, _cells.ToArray());

            _id = index * 100 * 100 + A.Id * 100 + B.Id;
        }  

        public void HandlePlayerExecutedTurn( int senderId ,c2s_execute_turn cmd)
        {
            Logger.Log($"Player {senderId} try to mark at {cmd.x} - {cmd.y}");

            // check legit of this execute
            if(!CheckLegitOfExecuteTurnCmd(senderId, cmd)) return;

            MarkCell(cmd.x, cmd.y);

            int nextTurnPlayerId = _isTurnOfA ? B.Id : A.Id;
            CmdSender.SendTurnStartCmd(A.Id, nextTurnPlayerId, _cells.ToArray());
            CmdSender.SendTurnStartCmd(B.Id, nextTurnPlayerId, _cells.ToArray());

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

    
}
