using System.Net;
using System.Net.WebSockets;
using System.Text;
using woke3;

namespace woke3
{
    public class Program
    {
        private static readonly string WEB_IP = "104.194.240.16";
        private static readonly int WEB_PORT = 8881;
        private static readonly string GAME_SERVER_ADDRESS = "0.tcp.ap.ngrok.io";
        private static readonly int GAME_SERVER_PORT = 11377;
        private static string _game_name = "Tictactoe";
        private static string _team_name = "tm45";
        private static GameServer gameServer;

        public static async Task Main(string[] args)
        {
            using var ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri($"ws://{WEB_IP}:{WEB_PORT}"), CancellationToken.None);

            bool IsConnected() => ws?.State == WebSocketState.Open;

            bool infoAccepted = false;
            if (IsConnected())
            {
                Console.WriteLine("Connected to web game. Start sending info.");
                await SendInfoPacket(ws);
            }

            while (IsConnected())
            {
                var buffer = await Receive(ws);
                if (buffer == null)
                {
                    Console.WriteLine("Disconnected from web game.");
                    await ws.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, "Disconnected", CancellationToken.None);
                    break;
                }

                if (!infoAccepted)
                {
                    var message = BitConverter.ToInt32(buffer[0..4]);
                    if (message == (int) ActionType.ACT_INFO_ACCEPTED)
                    {
                        infoAccepted = true;
                        Console.WriteLine("Info accepted. Start game server.");
                        gameServer = new GameServer(IPAddress.Any, 9001, 9002);
                        gameServer.Start();
                    }
                    else if (message == (int) ActionType.ACT_INFO_REJECTED)
                    {
                        Console.WriteLine("Info rejected. Please check your info.");
                        await ws.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, "Info rejected", CancellationToken.None);
                        break;
                    }
                    {
                        Console.WriteLine("Unknown message.");
                        await ws.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, "Disconnected", CancellationToken.None);
                        break;
                    }
                }
                else
                {
                    var action = (ActionType) BitConverter.ToInt32(buffer[0..4]);
                    int matchId = 0;
                    switch (action) 
                    {
                        case ActionType.ACT_CREATE_MATCH:
                            matchId = BitConverter.ToInt32(buffer[4..8]);
                            var uid1 = BitConverter.ToInt32(buffer[8..12]);
                            var uid2 = BitConverter.ToInt32(buffer[12..16]);
                            var keymatchLength = BitConverter.ToInt32(buffer[16..20]);
                            var keymatch = Encoding.UTF8.GetString(buffer[20..(20 + keymatchLength)]);
                            Console.WriteLine($"Create match {matchId} with key {keymatch}");
                            bool succeed = gameServer.CreateMatch(matchId, uid1, uid2, keymatch);
                            await SendCreateRespone(ws, succeed);
                            break;
                        case ActionType.ACT_REQUEST_UPDATE:
                            matchId = BitConverter.ToInt32(buffer[0..4]);
                            
                            break;
                    }
                }
            }

            // gameServer = new GameServer(IPAddress.Any, 9001, 9002);
            // Console.WriteLine("Connecting to web server...");
            // var client = new ServerSession(WEB_IP, WEB_PORT, gameServer);
            // client.Connect();
            // Console.WriteLine("Connected to web server!");
            // gameServer.Start();
            // Console.WriteLine($"Started game server on port {9001}!");
            // await Task.Delay(-1);
        }
        
        static async Task SendInfoPacket(ClientWebSocket ws)
        {
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
            
            await Send(ws, info.SelectMany(a => a).ToArray());
        }
        
        static async Task SendCreateRespone(ClientWebSocket ws, bool state)
        {
            var info = new List<byte[]>
            {
                BitConverter.GetBytes((int) ActionType.ACT_CREATE_MATCH_RESPONSE),
                BitConverter.GetBytes(state ? 1 : 0)
            };
            
            await Send(ws, info.SelectMany(a => a).ToArray());
        }
        
        static async Task Send(ClientWebSocket socket, byte[] data) =>
            await socket.SendAsync(data, WebSocketMessageType.Binary, true, CancellationToken.None);
        
        static async Task<byte[]?> Receive(ClientWebSocket socket)
        {
            var buffer = new byte[1024];
            var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
            return buffer[..result.Count];
        }
    }
}

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

enum ActionType
{
    ACT_INFO_REJECTED = 0,
    ACT_INFO_ACCEPTED = 1,
    ACT_CREATE_MATCH = 1,
    ACT_CREATE_MATCH_RESPONSE = 1,
    ACT_MATCH_RESULT = 2,
    ACT_UPDATE_MATCH = 3,
    ACT_MATCH_START = 3,
    ACT_REQUEST_UPDATE = 3
}