using Shared.Common;
using Shared.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{

    public class SessionManager : Singleton<SessionManager>
    {
        #region PROPERTIES

        private int _poolMax = 10;
        private int _poolInitCount = 5;

        private IObjectPool<Session> _sessionPool;
        private uint _sessionIndex = 0;

        private Dictionary<int, Session> _runningSessionMap = new(); 

        #endregion

        #region PUBLIC METHODS

        public void Start()
        {
            if (_sessionPool == null)
            {
                _sessionPool = new ObjectPool<Session>(_poolInitCount, CreateSession, HandleReleaseSession);
            }
        }

        public void StartSession(TcpClient tcpClient)
        {
            var session = _sessionPool.Get();
            if (session == null)
            {
                Logger.Log("[Error] Can't get Session instance from pool !");
                return;
            }


            session.Start((int)_sessionIndex, tcpClient);
            session.OnPacketReceived += HandleReceivedPacket;
            session.OnClosed += HandleSessionClosed;
            
            _runningSessionMap.Add(session.SessionId, session);

            _sessionIndex++;

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

            _runningSessionMap.Remove(session.SessionId);

            if(session.State == SessionState.Authenticated)
                Logger.Log($" {session.Player.Data.UserName} disconnected ");
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
