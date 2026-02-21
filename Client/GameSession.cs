using Shared.Logic;
using Shared.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class GameSession : Session
    {

        #region Send cmd to Server
        public void SendLoginCmd(string username, string password)
        {
            var cmd = new c2s_login(username, password);
            SendCmd(EPacketHeader.Login, cmd);
        }

        public void SendSignUpCmd(string username, string password)
        {
            var cmd = new c2s_signup(username, password);
            SendCmd(EPacketHeader.SignUp, cmd); 
        }

        public void SendFindMatchCmd()
        {
            var cmd = new c2s_signup();
            SendCmd(EPacketHeader.Find_Match, cmd);
        }

        public void SendExecuteTurnCmd(int x, int y)
        {
            var cmd = new c2s_execute_turn();
            SendCmd(EPacketHeader.Execute_Turn, cmd);
        }
       
        #endregion

        #region Revice from Server


        #endregion
    }
}
