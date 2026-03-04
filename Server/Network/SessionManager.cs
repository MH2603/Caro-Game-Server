using Server.Network;
using Shared.Common;
using Shared.Network;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace Server
{

    public class SessionManager : Singleton<SessionManager>
    {
        #region PROPERTIES

        private int _poolMax = 10;
        private int _poolInitCount = 5;

        private IObjectPool<Session> _sessionPool;
        private uint _sessionIndex = 0;

        private ConcurrentDictionary<int, Session> _runningSessionMap = new(); 
        private Action<Session>? OnSessionClosed;

        private PingTracker pingTraker;

        #endregion

        #region PUBLIC METHODS

        public void Start()
        {
            if (_sessionPool == null)
            {
                _sessionPool = new ObjectPool<Session>(_poolInitCount, CreateSession, HandleReleaseSession);
            }

            pingTraker = new PingTracker();
            pingTraker.Start();
        }

        public void StartSession(TcpClient tcpClient)
        {
            // get session from pool
            var session = _sessionPool.Get();
            if (session == null)
            {
                Logger.Log("[Error] Can't get Session instance from pool !");
                return;
            }

            // start session
            session.Start((int)_sessionIndex, tcpClient);
            session.OnPacketReceived += HandleReceivedPacket;
            session.OnClosed += HandleSessionClosed;

            // add to running session map
            _runningSessionMap.TryAdd(session.SessionId, session);
            _sessionIndex++;

            // register to ping tracker
            pingTraker.RegisterSession(session);    

            Logger.Log($"New session {session.SessionId} was started");
        }

        public void SendPacket(int playerId, Packet packet)
        {
            // find session base on player id
            // TODO: need a Dictionary to easy find
            foreach (var session in _runningSessionMap.Values)
            {
                if (session.State == SessionState.Authenticated &&
                    session.Player.Data.Id == playerId)
                {
                    session.SendPacket(packet);
                    return;
                }
            }
        }

        public void SendCmd<T>(int playerId, EPacketHeader header, T cmd) where T : struct, IByteSerializable
        {
            var packet = new Packet
            {
                Header = header,
                Data = cmd.ToBytes()
            };

            SendPacket(playerId, packet);   
        }

        public void RegisterSessionClosedCallback(Action<Session> callback)
        {
            OnSessionClosed += callback;    
        }

        public void UnregisterSessionClosedCallback(Action<Session> callback)
        {
            OnSessionClosed -= callback;
        }

        #endregion

        #region CALL BACK

        private void HandleReleaseSession(Session session)
        {
            
        } 

        private void HandleSessionClosed(Session session)
        {
            _sessionPool.Release(session);
            session.OnClosed -= HandleSessionClosed;
            session.OnPacketReceived -= HandleReceivedPacket;

            _runningSessionMap.TryRemove(session.SessionId, out _);

            pingTraker.UnregisterSession(session);

            OnSessionClosed?.Invoke(session);
        }

        private void HandleReceivedPacket(Session session, Packet packet)
        {
            PacketDispatcher.Instance.Dispatch(session, packet);
        }

        #endregion

        private Session CreateSession()
        {
            return new Session();
        }
        
    }
}
