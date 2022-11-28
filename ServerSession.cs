using System.Collections.Specialized;
using System.Text;
using NetCoreServer;

namespace woke3
{
    public class ServerSession : WsClient
    {
        private readonly GameServer _server;
        public readonly string GAME_SERVER_ADDRESS = "0.tcp.ap.ngrok.io";
        public readonly int GAME_SERVER_PORT = 11377;
        private readonly string _game_name = "Tictactoe";
        
        private bool _webAcceptInfo = false;
        
        public ServerSession(string address, int port, GameServer server) : base(address, port)
        {
            _server = server;
        }
        
        public override void OnWsConnecting(HttpRequest request)
        {
            request.SetBegin("GET", "/");
            request.SetHeader("Host", "localhost");
            request.SetHeader("Origin", "http://localhost");
            request.SetHeader("Upgrade", "websocket");
            request.SetHeader("Connection", "Upgrade");
            request.SetHeader("Sec-WebSocket-Key", Convert.ToBase64String(WsNonce));
            request.SetHeader("Sec-WebSocket-Protocol", "binary.ircv3.net");
            request.SetHeader("Sec-WebSocket-Version", "13");
            request.SetBody();
        }

        protected override void OnConnected()
        {
            Console.WriteLine("Connected with ID: " + Id);
            //sleep thread for 5 seconds and send message to web server
            
            new Thread(() =>
            {
                Thread.Sleep(1000);
                lock (this)
                {
                    var info = new List<byte[]>()
                    {
                        BitConverter.GetBytes(GAME_SERVER_ADDRESS.Length),
                        Encoding.ASCII.GetBytes(GAME_SERVER_ADDRESS),
                        BitConverter.GetBytes(GAME_SERVER_PORT),
                        BitConverter.GetBytes(_game_name.Length),
                        Encoding.ASCII.GetBytes(_game_name),
                        BitConverter.GetBytes(2),
                        Encoding.ASCII.GetBytes("aa"),
                        BitConverter.GetBytes(2),
                        Encoding.ASCII.GetBytes("bb")
                    };
                    Console.WriteLine("Send info to web server");
                    Console.WriteLine(SendPacket(1, info.SelectMany(a => a).ToArray()));
                }

                
            }).Start();
            base.OnConnected();
        }

        public override void OnWsReceived(byte[] buffer, long offset, long size)
        {
            try
            {
                Console.WriteLine("hsadas");
                OnReceivedInternal(buffer, offset, size);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void OnReceivedInternal(byte[] buffer, long offset, long size)
        {
            Console.WriteLine("SOMETHing");
            if (!_webAcceptInfo)
            {
                var accept = BitConverter.ToInt32(buffer[0..4]);
                if (accept == 1) _webAcceptInfo = true;
                else
                {
                    // convert byte array to string
                    var error = Encoding.ASCII.GetString(buffer[4..]);
                    Console.WriteLine(error);
                }
            }
            base.OnWsReceived(buffer, offset, size);
        }

        private bool SendPacket(int action, byte[] payload)
        {
            var data = new List<byte[]>()
            {
                BitConverter.GetBytes(action),
                payload
            };
            var packet = data.SelectMany(a => a).ToArray();
            long sent = SendBinary(packet);
            Console.WriteLine(sent + " " + packet.Length);
            return sent == packet.Length;
            // print packet
        }
    }
}
