using Shared.Logic;
using Shared.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Network
{
    public class PingNote
    {
        public int SessionId { get; set; }
        public float ElapsedSendPingTime { get; set; }
        public float ElapsedReceivePongTime { get; set; }

        public PingNote(int sessionId)
        {
            SessionId = sessionId;
            ElapsedSendPingTime = 0f;
            ElapsedReceivePongTime = 0f;
        }
    }

    public class PingTracker : IDisposable, IPacketHandler
    {
        // if the server doesn't receive a pong from the client within this time, it considers the client disconnected
        private float PongDelayThreshold = 15f; 
        private float PingCooldown = 5f; // per X seconds, a player can only send one ping

        Dictionary<Session, PingNote> _pingMap = new (); // sessionId -> last ping time
        

        CancellationTokenSource _cts;
        
        public void Start()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => TickLoopAsync());

            PacketDispatcher.Instance.RegisterHandler(EPacketHeader.ClientPong, this);  
        }

        public void Dispose()
        {
            _cts.Cancel();
            PacketDispatcher.Instance.UnregisterHandler(EPacketHeader.ClientPong);
        }

        public void RegisterSession(Session session)
        {
            _pingMap[session] = new PingNote(session.SessionId);    
        }

        public void UnregisterSession(Session session)
        {
            
            if (_pingMap.ContainsKey(session))
            {
                _pingMap.Remove(session);
            }
            else
            {
                Logger.LogError($"Trying to unregister session {session.SessionId} from PingTracker, but it was not found in the ping map.");
            }
        }

        async void TickLoopAsync()
        {
            var lastTime = DateTime.UtcNow;
            while (!_cts.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                var deltaTime = (float)(now - lastTime).TotalSeconds;
                lastTime = now;
                Tick(deltaTime);
                await Task.Delay(1000, _cts.Token);
            }
        }


        public void Tick(float detalTime)
        {

            foreach ( var session in _pingMap.Keys)
            {
                var note = _pingMap[session];   
                note.ElapsedSendPingTime += detalTime;
                note.ElapsedReceivePongTime += detalTime;

                if (note.ElapsedReceivePongTime > PongDelayThreshold)
                {
                    session.Close();
                    //CmdSender.SendLogoutResponse(session, false);   
                    return;
                }

                if (note.ElapsedSendPingTime > PingCooldown)
                {
                    SendPing(session);
                    note.ElapsedSendPingTime = 0f;
                }
               
            }
        }

        void SendPing(Session session)
        {
            s2c_ping cmd = new s2c_ping() { Timestamp = DateTimeOffset.UtcNow.Second };
            session.SendCmd(EPacketHeader.ServerPing, cmd);
        }

        public void HandlePacket(Session session, Packet packet)
        {
            if (packet.Header == EPacketHeader.ClientPong)
            {
                c2s_pong cmd = new c2s_pong(packet.Data);
                if ( _pingMap.TryGetValue( session,out var note))
                {
                    note.ElapsedReceivePongTime = 0f;
                    Logger.Log($"Received a pong from session {session.SessionId}, ping = {DateTimeOffset.UtcNow.Second - note.ElapsedSendPingTime}");
                }
                else
                {
                    Logger.LogError($"Received a pong from session {session.SessionId}, but it was not found in the ping map.");
                }
            }
        }
    }
}
