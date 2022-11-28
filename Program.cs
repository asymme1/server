using System.Net;
using System.Net.WebSockets;
using System.Text;
using woke3;

namespace woke3
{
    public class Program
    {
        private static readonly string WEB_IP = "127.0.0.1";
        private static readonly int WEB_PORT = 8881;
        private static readonly string GAME_SERVER_ADDRESS = "0.tcp.ap.ngrok.io";
        private static readonly int GAME_SERVER_PORT = 11377;
        private static string _game_name = "Tictactoe";
        private static string _team_name = "tm45";
        private static GameServer _server;

        public static async Task Main(string[] args)
        {
            using (var socket = new ClientWebSocket())
            {
                await socket.ConnectAsync(new Uri("ws://104.194.240.16:8881"), CancellationToken.None);

                var info = new List<byte[]>
                {
                    BitConverter.GetBytes(1),
                    BitConverter.GetBytes(GAME_SERVER_ADDRESS.Length),
                    Encoding.UTF8.GetBytes(GAME_SERVER_ADDRESS),
                    BitConverter.GetBytes(GAME_SERVER_PORT),
                    BitConverter.GetBytes(_game_name.Length),
                    Encoding.UTF8.GetBytes(_game_name),
                    BitConverter.GetBytes(2),
                    Encoding.UTF8.GetBytes("aa"),
                    BitConverter.GetBytes(_team_name.Length),
                    Encoding.UTF8.GetBytes(_team_name)
                };
                
                Console.Write("123123");
                //await Send(socket, info.SelectMany(a => a).ToArray());

                //_server = new GameServer(IPAddress.Any, 9001);
                //_server.Start();
                //await Task.Delay(-1);

                // while (true)
                // {
                //     if (socket.State == WebSocketState.CloseReceived) break;
                //     var buffer = new byte[1024];
                //     var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                //     if (result.MessageType == WebSocketMessageType.Close) break;
                //     var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                //     Console.WriteLine(message);
                // }
            }
        }
        
        static async Task Send(ClientWebSocket socket, byte[] data) =>
            await socket.SendAsync(data, WebSocketMessageType.Binary, true, CancellationToken.None);
    }
}
var gameServer = new GameServer(IPAddress.Any, 9001, 9002);
Console.WriteLine("Connecting to web server...");
var client = new ServerSession(WEB_IP, WEB_PORT, gameServer);
client.Connect();
Console.WriteLine("Connected to web server!");
gameServer.Start();
Console.WriteLine("Started game server!");
await Task.Delay(-1);

enum PacketType
{
    PKT_SPECIAL_RESET = -1,
    PKT_HI = 0,
    PKT_ID,
    PKT_BOARD,
    PKT_SEND,
    PKT_RECEIVE,
    PKT_ERROR,
    PKT_END
}