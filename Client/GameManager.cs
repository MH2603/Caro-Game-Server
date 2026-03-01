using System;
using Shared.GameLogic;
using Shared.Logic;
using Shared.Network;

namespace Client
{
    public class GameManager 
    {
        Player _player;
        ClientSession _session;

        // in-match fields
        bool _isPlayerTurn = false; // use to check current turn
        int _matchId = 0;
        int _opponentId = 0;    

        public Player Player { get { return _player; } }


        public GameManager(ClientSession gameSession) 
        {
            _session = gameSession;

            _session.OnPacketReceived += HandlePacket;

        }

        public void HandlePacket( Session session, Packet packet)
        {
            Logger.Log($"\n Client: Received packet with header = {packet.Header} , size = {packet.Data.Length}");
            switch(packet.Header)
            {
                case EPacketHeader.Login_Response:
                    HandleLoginRespone(packet);
                    break;
                case EPacketHeader.Match_Start:
                    HandleMatchStart(packet);
                    break;
                case EPacketHeader.Start_Turn:
                    HandleStartTurn(packet);
                    break;
                case EPacketHeader.Match_End:
                    HandleMatchEnd(packet);
                    break;
            }   
        }

        private void HandleMatchEnd(Packet packet)
        {
            s2c_match_end cmd = new s2c_match_end(packet.Data);

            if ( cmd.WasDraw)
            {
                Logger.Log($" Match end with result DRAW ");
            }
            else if( cmd.WinnerId == _player.Id)
            {
                Logger.Log($"Match end, you win :> ");
            }
            else
            {
                Logger.Log($"Match end, you lose :< ");
            }

            _isPlayerTurn = false;
            _opponentId = 0;
            _matchId = 0;

            _player.ChangeState(EPlayerState.Online);
        }

        private void HandleStartTurn(Packet packet)
        {
            s2c_start_turn cmd = new (packet.Data);

            RenderBoard(cmd.Cells);

            // check player's turn flag 
            _isPlayerTurn = cmd.NextTurnPlayerId == _player.Id ? true : false;

            if ( _isPlayerTurn)
            {
                Logger.Log("CLIENT: The opponent moved, let do your turn !");
            }

        }

        private void HandleLoginRespone(Packet packet)
        {
            byte[] data = packet.Data;
            s2c_login cmd = new s2c_login(data);

            switch (cmd.Result)
            {
                case 0:
                    InitPlayer(playerId: cmd.PlayerId);
                    Logger.Log(" Login success !");
                    break;
                case 1:
                    Logger.Log(" * Wrong username or password !");
                    break;
            }
        }

        void HandleMatchStart(Packet packet)
        {
            var cmd = new s2c_match_start(packet.Data);

            _player.ChangeState(EPlayerState.InMatch);
 
            _opponentId = cmd.Player_A_Id == Player.Id ? cmd.Player_B_Id : cmd.Player_A_Id;
            _matchId = cmd.MatchId;  

            Logger.Log( $" Started a match with player={ _opponentId} at match={_matchId}");
        }

        void InitPlayer(int playerId)
        {
            var data = new PlayerData
            {
                Id = playerId,  
            };

            _player = new Player(data);
        }

        void RenderBoard(Cell[] cells)
        {
            if (cells == null || cells.Length == 0)
            {
                Logger.Log("Board is empty or invalid.");
                return;
            }

            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;

            foreach (var cell in cells)
            {
                int x = (int)cell.Pos.X;
                int y = (int)cell.Pos.Y;

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }

            int size_x = maxX - minX + 1;
            int size_y = maxY - minY + 1;

            int[,] board = new int[size_x, size_y];

            foreach (var cell in cells)
            {
                int x = (int)cell.Pos.X;
                int y = (int)cell.Pos.Y;
                int ix = x - minX;
                int iy = y - minY;

                if (ix >= 0 && ix < size_x && iy >= 0 && iy < size_y)
                {
                    board[ix, iy] = cell.Status;
                }
            }

            Logger.Log("Current board:");

            for (int y = 0; y < size_y; y++)
            {
                for (int x = 0; x < size_x; x++)
                {
                    int status = board[x, y];
                    char c = status switch
                    {
                        1 => 'X',
                        2 => 'O',
                        _ => '.'
                    };

                    ConsoleColor color = status switch
                    {
                        1 => ConsoleColor.Cyan,
                        2 => ConsoleColor.Magenta,
                        _ => ConsoleColor.DarkGray
                    };

                    Console.ForegroundColor = color;
                    Console.Write(c);
                    Console.ResetColor();

                    if (x < size_x - 1)
                        Console.Write(' ');
                }
                Console.WriteLine();
            }

            Console.ResetColor();
        }

        public void TryExecuteTurn(int pos_x, int pos_y)
        {
            if ( _player.State != EPlayerState.InMatch ||
                 !_isPlayerTurn)
            {
                Logger.Log($"Cannt execute turn State={_player.State} | IsTurn={_isPlayerTurn}");
                return;
            }

            Logger.Log($"CLIENT: marked at {pos_x} {pos_y}");

            //_isPlayerTurn = false;
            _session.SendExecuteTurnCmd(_matchId, pos_x, pos_y);
        }
    }
}
