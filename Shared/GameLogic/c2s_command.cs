using Shared.Common;
using Shared.Network;
using System.Runtime.InteropServices;
using System.Text;

namespace Shared.Logic
{

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct c2s_login : IByteSerializable
    {
        public int UserNameSize;
        public byte[] UserName;
        public int PasswordSize;
        public byte[] Password;

        public c2s_login(string username, string password)
        {
            UserName = Encoding.UTF8.GetBytes(username);
            Password = Encoding.UTF8.GetBytes(password);
            UserNameSize = UserName.Length;
            PasswordSize = Password.Length;
        }

        public c2s_login(byte[] bytes)
        {
            int offset = 0;
            UserNameSize = BitConverter.ToInt32(bytes, offset); offset += 4;
            UserName = new byte[UserNameSize];
            Array.Copy(bytes, offset, UserName, 0, UserNameSize); offset += UserNameSize;
            PasswordSize = BitConverter.ToInt32(bytes, offset); offset += 4;
            Password = new byte[PasswordSize];
            Array.Copy(bytes, offset, Password, 0, PasswordSize);
        }

        public byte[] ToBytes()
        {
            var list = new List<byte>();
            list.AddRange(BitConverter.GetBytes(UserNameSize));
            list.AddRange(UserName ?? Array.Empty<byte>());
            list.AddRange(BitConverter.GetBytes(PasswordSize));
            list.AddRange(Password ?? Array.Empty<byte>());
            return list.ToArray();
        }

        public string GetUserName() => UserName != null ? Encoding.UTF8.GetString(UserName, 0, UserNameSize) : string.Empty;
        public string GetPassword() => Password != null ? Encoding.UTF8.GetString(Password, 0, PasswordSize) : string.Empty;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct c2s_signup : IByteSerializable
    {
        public int UsernameSize;
        public byte[] Username;
        public int PasswordSize;
        public byte[] Password;

        public c2s_signup(string user, string pw)
        {
            Username = Encoding.UTF8.GetBytes(user);
            UsernameSize = Username.Length;
            Password = Encoding.UTF8.GetBytes(pw);
            PasswordSize = Password.Length;
        }

        public c2s_signup(byte[] bytes)
        {
            int offset = 0;
            UsernameSize = BitConverter.ToInt32(bytes, offset); offset += 4;
            Username = new byte[UsernameSize];
            Array.Copy(bytes, offset, Username, 0, UsernameSize); offset += UsernameSize;
            PasswordSize = BitConverter.ToInt32(bytes, offset); offset += 4;
            Password = new byte[PasswordSize];
            Array.Copy(bytes, offset, Password, 0, PasswordSize);
        }

        public string GetUsername() => Encoding.UTF8.GetString(Username, 0, UsernameSize);
        public string GetPassword() => Encoding.UTF8.GetString(Password, 0, PasswordSize);

        public byte[] ToBytes()
        {
            var list = new List<byte>();
            list.AddRange(BitConverter.GetBytes(UsernameSize));
            list.AddRange(Username ?? Array.Empty<byte>());
            list.AddRange(BitConverter.GetBytes(PasswordSize));
            list.AddRange(Password ?? Array.Empty<byte>());
            return list.ToArray();
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct c2s_FindMatch : IByteSerializable
    {
        public c2s_FindMatch(byte[] bytes) { }

        public byte[] ToBytes() => Array.Empty<byte>();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct c2s_execute_turn : IByteSerializable
    {
        public int MatchId;
        public int x;
        public int y;

        public c2s_execute_turn(int matchId, int x, int y)
        {
            MatchId = matchId;
            this.x = x;
            this.y = y;
        }

        public c2s_execute_turn(byte[] bytes)
        {
            int offset = 0;

            MatchId = BitConverter.ToInt32(bytes, offset); offset += 4;
            x = BitConverter.ToInt32(bytes, offset); offset += 4;
            y = BitConverter.ToInt32(bytes, offset); offset += 4;
        }

        public byte[] ToBytes() => StructByteConverter.ToBytes(this);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct c2s_pong : IByteSerializable
    {
        public long Timestamp;
        public c2s_pong(long timestamp)
        {
            Timestamp = timestamp;
        }
        public c2s_pong(byte[] bytes) 
        {
            Timestamp = BitConverter.ToInt64(bytes, 0);
        }
        public byte[] ToBytes() => StructByteConverter.ToBytes(this);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct c2s_logout : IByteSerializable
    {
        public c2s_logout(byte[] bytes) { }
        public byte[] ToBytes() => Array.Empty<byte>();
    }
}
