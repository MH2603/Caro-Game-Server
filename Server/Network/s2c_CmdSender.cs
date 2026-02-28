using Server.GameLogic;
using Shared.GameLogic;
using Shared.Logic;
using Shared.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server.Network
{
    // build packet and send to client
    public class CmdSender
    {
        public static void SendLoginFault(Session session)
        {
            s2c_login s2C_Login = new s2c_login
            {
                Result = 1,
                PlayerId = 1,   
            };

            Packet packet = new Packet
            {
                Header = EPacketHeader.Login_Response,
                Data = s2C_Login.ToBytes()
            };

            session.SendPacket(packet);
        }

        public static void SendLoginResponse(int playerId, bool success)
        {
            s2c_login s2C_Login = new s2c_login
            {
                Result = success ? 0 : 1,
                PlayerId = playerId
            };

            Packet packet = new Packet
            {
                Header = EPacketHeader.Login_Response,
                Data = s2C_Login.ToBytes()
            };

            SessionManager.Instance.SendPacket(playerId, packet);
        }

        public static void SendMatchStartCmd(int matchId, int player_A_Id, int player_B_Id)
        {
            s2c_match_start cmd = new s2c_match_start(matchId, player_A_Id, player_B_Id);
            
            Packet packet = new Packet
            {
                Header = EPacketHeader.Match_Start,
                Data = cmd.ToBytes()
            };

            SessionManager.Instance.SendPacket(player_A_Id, packet);
            SessionManager.Instance.SendPacket(player_B_Id, packet);
        }


        public static void SendSignUpFaultCmd(Session session)
        {
            s2c_signup_result cmd = new s2c_signup_result
            {
                Result = 0
            };

            Packet packet = new Packet
            {
                Header = EPacketHeader.SignUp_Response,
                Data = cmd.ToBytes()
            };

            session.SendPacket(packet);
        }

        public static void SendTurnStartCmd(int receiverId , int nextTurnPlayerId, Cell[] cells)
        {
            var cmd = new s2c_start_turn(nextTurnPlayerId, cells);

            Packet packet = new Packet
            {
                Header = EPacketHeader.Start_Turn,
                Data = cmd.ToBytes()
            };

            SessionManager.Instance.SendPacket(receiverId, packet);
        }

        public static void SendMatchEnd( int receiverId, bool wasDraw, int winnerId)
        {
            var cmd = new s2c_match_end
            {
                WasDraw = wasDraw,
                WinnerId = winnerId
            };

            SessionManager.Instance.SendCmd(receiverId, EPacketHeader.Match_End, cmd);
        }

    }
}
