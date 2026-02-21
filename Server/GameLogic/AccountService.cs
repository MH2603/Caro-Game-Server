using Shared.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.GameLogic
{
    public interface IAccountService
    {

    }

    public class AccountService : IAccountService, IPacketHandler
    {

        public AccountService() 
        {
            RegisterWithPacketDispatch(EPacketHeader.Login);
            RegisterWithPacketDispatch(EPacketHeader.SignUp);
        }

        public void HandlePacket(Session session, Packet packet)
        {
            switch (packet.Header)
            {
                case EPacketHeader.Login:

                    break;
            }
        }

        void RegisterWithPacketDispatch(EPacketHeader packetHeader)
        {
            PacketDispatcher.Instance.RegisterHandler(packetHeader, this);
        }

        void HandleLoginPacket(Session session, Packet packet) 
        { 

        }
    }
}
