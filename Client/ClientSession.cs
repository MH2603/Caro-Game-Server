using Shared.Logic;
using Shared.Network;

namespace Client
{
    public class ClientSession : Session
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
            var cmd = default(c2s_FindMatch);
            SendCmd(EPacketHeader.Find_Match, cmd);
        }

        public void SendExecuteTurnCmd(int matchId, int x, int y)
        {
            var cmd = new c2s_execute_turn(matchId, x, y);
            SendCmd(EPacketHeader.Execute_Turn, cmd);
        }
       
        #endregion

        #region Revice from Server


        #endregion
    }
}
