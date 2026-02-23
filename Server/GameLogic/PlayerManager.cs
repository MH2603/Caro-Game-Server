using Server.Network;
using Shared.GameLogic;
using Shared.Logic;
using Shared.Network;
using System.Collections.Concurrent;
using System.Numerics;
using System.Text;

namespace Server.GameLogic
{
    public interface IPlayerService
    {
        Player GetRandomPlayerByState(EPlayerState state, out int foundCount);

        Player GetPlayer(int playerId);
    }

    public class PlayerManager : IPacketHandler, IPlayerService
    {
        private ConcurrentDictionary<int, Player> _playerMap = new();


        // ref other services
        IPlayerRepository PlayerRepository { get; set; }

        public PlayerManager( IPlayerRepository playerRepository)
        {   
            PlayerRepository = playerRepository;

            InitPlayers();

            // hanler cmd from Clients
            RegisterWithPacketDispatch(EPacketHeader.Login);
            RegisterWithPacketDispatch(EPacketHeader.SignUp);
        }

        async void InitPlayers()
        {
            PlayerData[] playerDatas = await PlayerRepository.GetAllAsync();

            for (int i = 0; i < playerDatas.Length; i++) 
            {
                var player = new Player(playerDatas[i]);
                _playerMap.TryAdd(player.Id, player); 
            }
        }

        void RegisterWithPacketDispatch(EPacketHeader packetHeader)
        {
            PacketDispatcher.Instance.RegisterHandler(packetHeader, this);
        }

        bool WasExistPlayer( string username)
        {
            foreach (var player in _playerMap.Values)
            {
                if(player.Data.UserName.Equals(username))
                {
                    return true;
                }
            }

            return false;
        }


        #region Player Service

        public Player GetRandomPlayerByState(EPlayerState state, out int foundCount)
        {
            Player[] players = _playerMap.Values.Where(x => x.State == state).ToArray();
            foundCount = players.Length;

            if (foundCount == 0) return null;

            var ran = new Random();

            return players[ran.Next(foundCount)];
        }

        public Player GetPlayer(int playerId)
        {
            return _playerMap.ContainsKey(playerId) ? _playerMap[playerId] : null;
        }

        #endregion

        #region Handle Packet
        public void HandlePacket(Session session, Packet packet)
        {
            switch (packet.Header)
            {
                case EPacketHeader.Login:
                    HandleLogin(session, packet);
                    break;
                case EPacketHeader.SignUp:
                    HandleSignUp(session, packet);
                    break;
            }
        }

        private async void HandleSignUp(Session session, Packet packet)
        {
            var cmd = new c2s_signup(packet.Data);

            var userName = cmd.GetUsername();
            var pw = cmd.GetPassword();

            if ( WasExistPlayer( userName) )
            {
                CmdSender.SendSignUpFaultCmd(session.Player.Id);
                return;
            }

            var playerData = new PlayerData
            {
                UserName = userName,
                Password = pw,
                CreatedDate  = DateTime.Now, 
                LastLoginDate = DateTime.Now,   
            };

            try
            {
                await PlayerRepository.AddAsync(playerData);

                var player = new Player(playerData);

                session.BindPlayer(player);
                player.ChangeState(EPlayerState.Online);

                // Send a command to player
                CmdSender.SendLoginResponse(player.Id, success: true);

                Logger.Log($"Player {userName} was signup success !");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
            }
            
        }

        

        void HandleLogin(Session session, Packet packet)
        {
            var cmd = new c2s_login(packet.Data);
            var userName = cmd.GetUserName();
            var pw = cmd.GetPassword();

            foreach (var player in _playerMap.Values )
            {
                if (player.Data.UserName == userName)
                {
                    if (player.Data.Password == pw)
                    {
                        session.BindPlayer(player);
                        player.ChangeState(EPlayerState.Online);

                        // Send a command to player
                        CmdSender.SendLoginResponse(player.Id, success: true);
                        
                        Logger.Log($"Player {userName} was login !");

                        return;
                    }
                    
                }
                
            }

            // do something to player know pass or user wrong
            CmdSender.SendLoginFault(session);

        }

        

        #endregion


    }
}
