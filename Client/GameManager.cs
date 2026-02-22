using Shared.GameLogic;
using Shared.Logic;
using Shared.Network;

namespace Client
{
    public class GameManager 
    {
        Player _player;
        GameSession _session;

        public Player Player { get { return _player; } }


        public GameManager(GameSession gameSession) 
        {
            _session = gameSession;

            _session.OnPacketReceived += (s, p) => HandlePacket(p);
        }

        public void HandlePacket(Packet packet)
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

        }

        void InitPlayer(int playerId)
        {
            var data = new PlayerData
            {
                Id = playerId,  
            };

            _player = new Player(data);
        }

       
    }
}
