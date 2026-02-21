using System.Runtime.InteropServices;

namespace Shared.Network
{

    public enum EPacketHeader : uint
    {
        None = 0,

        // c2s
        Disconnect = 1,
        Login = 2,
        SignUp = 3,
        Logout = 4,

        Find_Match = 100,
        Execute_Turn,

        // s2c
        Login_Response = 200,
        SignUp_Response,
        Start_Turn,
        Match_Start ,
        Match_End ,
    }

    [StructLayout( LayoutKind.Sequential, Pack =1)]
    public struct Packet
    {
        public EPacketHeader Header;
        public byte[] Data;
    }

    // read byte[] from client and convert to readable datas for Server
    // 1 packet = [Length=4][PacketId=4][Payload Bytes]
    public class PacketReader
    {
        private const int MAX_PACKET_SIZE = 64 * 1024; // 64KB limit
        public const int LENGTH_SIZE = 4;
        public const int PACK_HEADER_SIZE = 4;

        private List<byte> _cacheBuffer = new();
        public async Task<bool> ReadAsync(Stream stream, Action<Packet> callback, CancellationToken clt)
        {

            var bytes = new byte[MAX_PACKET_SIZE];
            int bytesRead =  await stream.ReadAsync(bytes, 0, bytes.Length, clt);

           
            if (bytesRead <= 0) return false; // disconnect

            _cacheBuffer.AddRange(bytes[0..bytesRead]);

            // if enough bit to find the length of packet
            while (_cacheBuffer.Count > LENGTH_SIZE)
            {
                int offset = 0;

                int packetLength = BitConverter.ToInt32(bytes, offset);
                offset += 4;

                // break if not enought data for packet
                if (_cacheBuffer.Count < packetLength) break;

                // convert data to packet
                var packet = new Packet();
                packet.Header = (EPacketHeader) BitConverter.ToInt32(bytes, offset);
                offset += 4;
                int packetDataSize = packetLength - LENGTH_SIZE - PACK_HEADER_SIZE;
                packet.Data = new byte[packetDataSize];
                Array.Copy(_cacheBuffer.ToArray(), offset, packet.Data, 0, packetDataSize);
                callback(packet);

                // cut first part which was converted to packet
                _cacheBuffer.RemoveRange( 0, packetLength);
            }
           

            return true;
        }
    }



    public interface IPacketHandler
    {
        void HandlePacket(Session session, Packet packet);
    }

    public class PacketDispatcher : Singleton<PacketDispatcher>
    {
        private Dictionary<EPacketHeader, IPacketHandler> _handlerMap = new();

        public void RegisterHandler( EPacketHeader header,  IPacketHandler handler)
        {
            if ( _handlerMap.ContainsKey(header))
            {
                Logger.Log($"[BUG] {header} was registered in map !!!");
            }
            else
            {
                _handlerMap.Add(header, handler);
            }
        }

        public void UnregisterHandler( EPacketHeader header )
        {
            if ( _handlerMap.ContainsKey(header))
            {
                _handlerMap.Remove(header);
            }
        }

        public void Dispatch(Session session, Packet packet)
        {
            if ( _handlerMap.ContainsKey(packet.Header) )
            {
                _handlerMap[packet.Header].HandlePacket(session, packet);
            }
            
        }
    }
}
