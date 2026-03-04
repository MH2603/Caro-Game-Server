using Server.Network;
using Shared.GameLogic;
using Shared.Logic;
using Shared.Network;

namespace Server.GameLogic
{
    public class Match : IDisposable
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

            _id = index * 100 * 100 + A.Id * 100 + B.Id;
            CmdSender.SendMatchStartCmd(Id, playerA.Data.Id, playerB.Data.Id);

            A.ChangeState(EPlayerState.InMatch);
            B.ChangeState(EPlayerState.InMatch);

            // auto mark at (0,0) for player A
            _cells = new List<Cell>();
            MarkCell(0, 0, 1);
            _isTurnOfA = false;
            
            int nextTurnPlayerId = _isTurnOfA ? A.Id : B.Id;
            CmdSender.SendTurnStartCmd( A.Id, nextTurnPlayerId, _cells.ToArray());
            CmdSender.SendTurnStartCmd( B.Id, nextTurnPlayerId, _cells.ToArray());

            //test
            //var winnerId = A.Id;
            //CmdSender.SendMatchEnd(A.Id, false, winnerId);
            //CmdSender.SendMatchEnd(B.Id, false, winnerId);
        }

        public void Dispose()
        {
            A = null;
            B = null;
            _cells.Clear();
            OnMatchEnd = null;
        }

        public void HandlePlayerExecutedTurn( int senderId ,c2s_execute_turn cmd)
        {
            Logger.Log($"Player {senderId} try to mark at {cmd.x} - {cmd.y}");

            // check legit of this execute
            if(!CheckLegitOfExecuteTurnCmd(senderId, cmd)) return;

            MarkCell(cmd.x, cmd.y, _isTurnOfA ? 1 : 2);

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
                EndMatch(_isTurnOfA ? A.Id : B.Id);
            }
                
        }

        void EndMatch(int winnerId)
        {

            if (_isTurnOfA)
            {
                A.Data.WinMatch++;
                B.Data.LostMatch++;
            }
            else
            {
                A.Data.LostMatch++;
                B.Data.WinMatch++;
            }

            var repoService = ServiceLocator.GetService<IPlayerRepository>();
            repoService.UpdateAsync(A.Data);
            repoService.UpdateAsync(B.Data);

            A.ChangeState(EPlayerState.Online);
            B.ChangeState(EPlayerState.Online);

            CmdSender.SendMatchEnd(A.Id, false, winnerId);
            CmdSender.SendMatchEnd(B.Id, false, winnerId);

            OnMatchEnd?.Invoke(this);
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

                var cellIndex = GetCellIndex(pos_x, pos_y);

                if ( cellIndex >= 0 &&
                    _cells[cellIndex].Status == markValue)
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

            var index = GetCellIndex(cmd.x, cmd.y);
            if( index >= 0 && _cells[index].Status != 0)
            {
                return false;
            }

            return true;
        }

        int GetCellIndex( int x, int y)
        {
            for ( int i=0; i < _cells.Count; i++)
            {
                if (_cells[i].Pos.X == x && _cells[i].Pos.Y == y)
                {
                    return i;
                }
            }

            return -1;
        }

        void MarkCell(int x, int y, int markValue)
        {
            var index = GetCellIndex(x, y);
            if (index < 0)
            {
                // create a new cell
                var cell = new Cell(x, y, markValue);
                _cells.Add(cell);
            }
            else
            {
                var tempCell = _cells[index];
                tempCell.Status = markValue;
                _cells[index] = tempCell;
            }
        }

        public  void HandleSessionClosed(int disconnectedPlayerId)
        {
            if ( disconnectedPlayerId != A.Id &&
                 disconnectedPlayerId != B.Id)
            {
                return;
            }

            bool wasPlayerADisconnected = disconnectedPlayerId == A.Id;
            int winnerId = wasPlayerADisconnected ? B.Id : A.Id;

            EndMatch(winnerId);
        }


    }

    
}
