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
            // Send information to Web Server
            // convert string to byte
        }

        public override void OnWsConnecting(HttpRequest request)
        {
            //base.OnWsConnecting(request);
            //
        }

        protected override void OnConnected()
        {
            //sleep thread for 5 seconds and send message to web server
            
            new Thread(() =>
            {
                Thread.Sleep(5000);
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
                SendPacket(1, info.SelectMany(a => a).ToArray());
            }).Start();
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
        }

        private bool SendPacket(int action, byte[] payload)
        {
            var data = new List<byte[]>()
            {
                BitConverter.GetBytes(action),
                payload
            };
            var packet = data.SelectMany(a => a).ToArray();
            return Send(packet) == packet.Length;
            // print packet
        }
    }
}
