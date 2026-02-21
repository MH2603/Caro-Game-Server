using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class NetworkListener : IDisposable
    {
       
        private int _port = 2003;

        private TcpListener _tcpListener;
        private int _clientMax = 10;

        private CancellationTokenSource _cts = new();

        public NetworkListener() 
        {
            
        }

        public void Start()
        {
            Task.Run(ListenLoop);
        }

        public void Close()
        {
            _cts.Cancel();
            _tcpListener.Dispose();
        }

        public void Dispose()
        {
            Close();
            _cts.Dispose();
        }

        private async void ListenLoop()
        {
            _tcpListener = new TcpListener(IPAddress.Any, 2003);

            _tcpListener.Start(_clientMax);

            while (!_cts.IsCancellationRequested)
            {
                TcpClient tcpClient = await _tcpListener.AcceptTcpClientAsync();
                SessionManager.Instance.StartSession(tcpClient);
            }
        }


        
    }
}
