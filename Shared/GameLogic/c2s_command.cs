using Shared.Network;
using System.Runtime.InteropServices;
using System.Text;

namespace Shared.Logic
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct c2s_login
    {
        public int UserNameSize;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = ConstData.STR_BYTES_MAX_SIZE)]
        public byte[] UserName;
        public int PasswordSize;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = ConstData.STR_BYTES_MAX_SIZE)]
        public byte[] Password;

        public c2s_login(string username, string password) 
        {
            UserName = Encoding.UTF8.GetBytes(username);
            Password = Encoding.UTF8.GetBytes(password);
            UserNameSize = UserName.Length;
            PasswordSize = Password.Length;
        }

        //public byte[] ToBytes()
        //{
        //    byte[] bytes = new byte[4 + 4 + PasswordSize + UserNameSize];
        //    int offset = 0;

        //    Array.Copy(StructByteConverter.ToBytes(UserNameSize), 0, bytes, offset, 4);
        //    offset += 4;

        //    Array.Copy(UserName, 0, bytes, offset, UserNameSize);
        //    offset += UserNameSize;

        //    Array.Copy(StructByteConverter.ToBytes(PasswordSize), 0, bytes, offset, 4);
        //    offset += 4;

        //    Array.Copy(Password, 0, bytes, offset, PasswordSize);
        //    offset += PasswordSize;

        //    return bytes;   
        //}

        //public void FromByte(byte[] bytes)
        //{

        //    UserNameSize = BitConverter.ToInt32(bytes[0..4]);
        //    UserName = new byte[UserNameSize];
        //    Array.Copy(bytes, 4, UserName, 0, UserNameSize);

        //    int offset = 4 + UserNameSize;
        //    PasswordSize = BitConverter.ToInt32(bytes[offset..(offset + 4)]);
        //    offset += 4;    
        //    Password = new byte[PasswordSize];
        //    Array.Copy(bytes, offset, Password, 0, PasswordSize);
        //}
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct c2s_signup
    {
        public int UsernameSize;
        [MarshalAs( UnmanagedType.ByValArray, SizeConst = ConstData.STR_BYTES_MAX_SIZE)]
        public byte[] Username;
        public int PasswordSize;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = ConstData.STR_BYTES_MAX_SIZE)]
        public byte[] Password;

        public c2s_signup(string user, string pw)
        {
            Username = Encoding.UTF8.GetBytes(user);
            UsernameSize = Username.Length;
            Password = Encoding.UTF8.GetBytes(pw);
            PasswordSize = Password.Length;
        }

        public string GetUsername()
        {
            return Encoding.UTF8.GetString(Username, 0, UsernameSize);   
        }

        public string GetPassword()
        {

            return Encoding.UTF8.GetString(Password, 0, PasswordSize);
        }


    }

    [StructLayout( LayoutKind.Sequential, Pack =   1)]
    public struct c2s_FindMatch()
    {

    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct c2s_execute_turn
    {
        public int x;
        public int y;

        public c2s_execute_turn(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }
}
