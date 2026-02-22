using Shared.Common;
using Shared.Network;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Shared.Logic
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct s2c_login : IByteSerializable
    {
        // 0: login success, 1: wrong username or pw
        public int Result;
        public int PlayerId;

        public s2c_login(byte[] bytes)
        {
            Result = BitConverter.ToInt32(bytes, 0);
            PlayerId = BitConverter.ToInt32(bytes, 4);
        }

        public byte[] ToBytes() => StructByteConverter.ToBytes(this);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct s2c_match_start : IByteSerializable
    {
        public int Player_A_Id;
        public int Player_B_Id;

        public s2c_match_start(byte[] bytes)
        {
            Player_A_Id = BitConverter.ToInt32(bytes, 0);
            Player_B_Id = BitConverter.ToInt32(bytes, 4);
        }

        public byte[] ToBytes() => StructByteConverter.ToBytes(this);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct s2c_player_info : IByteSerializable
    {
        public int PlayerId;
        public int UserNameSize;
        public byte[] UserName;

        public s2c_player_info(int playerId, string username)
        {
            PlayerId = playerId;
            UserName = Encoding.UTF8.GetBytes(username);
            UserNameSize = UserName.Length;
        }

        public s2c_player_info(byte[] bytes)
        {
            int offset = 0;
            PlayerId = BitConverter.ToInt32(bytes, offset); offset += 4;
            UserNameSize = BitConverter.ToInt32(bytes, offset); offset += 4;
            UserName = new byte[UserNameSize];
            Array.Copy(bytes, offset, UserName, 0, UserNameSize);
        }

        public byte[] ToBytes()
        {
            var list = new List<byte>();
            list.AddRange(BitConverter.GetBytes(PlayerId));
            list.AddRange(BitConverter.GetBytes(UserNameSize));
            list.AddRange(UserName ?? Array.Empty<byte>());
            return list.ToArray();
        }

        public string GetUserName() => UserName != null ? Encoding.UTF8.GetString(UserName, 0, UserNameSize) : string.Empty;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct s2c_signup_result : IByteSerializable
    {
        public int Result;  // 0: fault

        public s2c_signup_result(byte[] bytes)
        {
            Result = BitConverter.ToInt32(bytes, 0);
        }

        public byte[] ToBytes() => StructByteConverter.ToBytes(this);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct s2c_start_turn : IByteSerializable
    {
        public uint CellArraySize;
        public Cell[] Cells;

        public s2c_start_turn(Cell[] cells)
        {
            Cells = cells;
            CellArraySize = (uint)cells.Length;
        }

        public s2c_start_turn(byte[] bytes)
        {
            int offset = 0;
            CellArraySize = BitConverter.ToUInt32(bytes, offset); offset += 4;
            Cells = new Cell[CellArraySize];
            for (int i = 0; i < CellArraySize; i++)
            {
                Cells[i] = new Cell(bytes.AsSpan(offset));
                offset += Cell.SizeBytes;
            }
        }

        public byte[] ToBytes()
        {
            var list = new List<byte>();
            list.AddRange(BitConverter.GetBytes(CellArraySize));
            if (Cells != null)
                foreach (var c in Cells)
                    list.AddRange(c.ToBytes());
            return list.ToArray();
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Cell : IByteSerializable
    {
        public const int SizeBytes = 12;  // Vector2 (8) + int (4)
        public Vector2 Pos;
        public int Status;  // 0: empty | 1: player 01 | 2: player 02

        public Cell(ReadOnlySpan<byte> bytes)
        {
            Pos = new Vector2(
                BitConverter.ToSingle(bytes),
                BitConverter.ToSingle(bytes.Slice(4)));
            Status = BitConverter.ToInt32(bytes.Slice(8));
        }

        public byte[] ToBytes()
        {
            var list = new List<byte>();
            list.AddRange(BitConverter.GetBytes(Pos.X));
            list.AddRange(BitConverter.GetBytes(Pos.Y));
            list.AddRange(BitConverter.GetBytes(Status));
            return list.ToArray();
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct s2c_match_end : IByteSerializable
    {
        public bool WasDraw;
        public int WinnerId;

        public s2c_match_end(byte[] bytes)
        {
            WasDraw = BitConverter.ToBoolean(bytes, 0);
            WinnerId = BitConverter.ToInt32(bytes, 4);
        }

        public byte[] ToBytes() => StructByteConverter.ToBytes(this);
    }
}
