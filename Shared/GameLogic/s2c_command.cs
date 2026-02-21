using Shared.Network;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Shared.Logic
{
    

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct s2c_login
    {
        // 0: login success
        // 1: wrong username or pw
        public int Result;
        public int PlayerId;
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct s2c_match_start
    {
        public int Player_A_Id;
        //[MarshalAs(UnmanagedType.ByValArray, SizeConst = ConstData.STR_BYTES_MAX_SIZE)]
        //public byte[] Player_A_UserName;
        public int Player_B_Id;
        //[MarshalAs(UnmanagedType.ByValArray, SizeConst = ConstData.STR_BYTES_MAX_SIZE)]
        //public byte[] Player_B_UserName;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct s2c_player_info
    {
        public int PlayerId;
        public int UserNameSize;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = ConstData.STR_BYTES_MAX_SIZE)]
        public byte[] UserName;

        public s2c_player_info( int playerId, string username)
        {
            PlayerId = playerId;
            UserName = Encoding.UTF8.GetBytes( username );  
            UserNameSize = UserName.Length;
        }

    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct s2c_signup_result
    {
        public int Result;  // 0: fault
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct s2c_start_turn 
    {
        public uint CellArraySize;
        public Cell[] Cells;

        public s2c_start_turn(Cell[] cells ) 
        {
            Cells = cells;
            CellArraySize = (uint)cells.Length;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack =1)]
    public struct Cell
    {
        public Vector2 Pos; // pos of cell on board
        public int Status; // 0 : empty | 1 : mark by player 01 | 2: mark by player 02

    }

    public struct s2c_match_end 
    {
        public bool WasDraw;
        public int WinnerId;
    }


}
