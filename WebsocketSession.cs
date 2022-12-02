using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using NetCoreServer;

namespace woke3
{
    public class WebsocketSession : WsSession
    {
        private static readonly string WebGameWS = "ws://104.194.240.16";
        private static readonly bool IsDevServer = true;
        private static readonly string ServerAddress = IsDevServer ? "tcp://0.tcp.ap.ngrok.io" : "";
        public WebsocketSession(WsServer server) : base(server) {}

        public override void OnWsConnected(HttpRequest req)
        {
            Console.WriteLine($"A new client connected with {Id}.");
            base.OnWsConnected(req);
        }

        public override void OnWsReceived(byte[] buffer, long offset, long size)
        {
            try
            {
                OnReceivedInternal(buffer);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
            base.OnWsReceived(buffer, offset, size);
        }

        private void OnReceivedInternal(byte[] buffer)
        {
            var option = new JsonSerializerOptions()
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            };
            string message = Encoding.UTF8.GetString(buffer[0..^1]);
            Console.WriteLine(message);
            // convert string to json
            // find the last } in buffer
            int lastCurlyBrace = message.LastIndexOf('}');
            var tmp = buffer[0..(lastCurlyBrace + 1)];
            var tmp2 = Encoding.ASCII.GetString(buffer);
            JsonObject? data = JsonSerializer.Deserialize<JsonObject>(tmp, option);
            //JsonNode? data = JsonNode.Parse(message);
            // check if data is null
            if (data == null)
            {
                Console.WriteLine("data is null");
                return;
            }
            
            if (data["action"] != null)
            {
                Enum.TryParse(data["action"]?.ToString(), out ActionType action);
                switch (action)
                {
                    case ActionType.ActCreateMatch:
                        int.TryParse(data["match"]?.ToString(), out int matchId);
                        int.TryParse(data["id1"]?.ToString(), out int uid1);
                        int.TryParse(data["id2"]?.ToString(), out int uid2);
                        string password = data["passwd"]?.ToString() ?? "Unknown";
                        CreateAndSendCreateState(matchId, uid1, uid2, password);
                        break;
                    case ActionType.ActUpdateMatch:
                        break;
                }
            }
        }

        private void CreateAndSendCreateState(int matchId, int uid1, int uid2, string password)
        {
            int validPort = FindNextValidPort();
            var gameServer = new GameServer(IPAddress.Any, validPort, 9000);
            gameServer.Start();
            Console.WriteLine($"New game server started on port {validPort}");

            bool state = gameServer.CreateMatch(matchId, uid1, uid2, password);
            JsonObject data = new JsonObject();
            if (state)
            {
                data.Add("result", 1);
                data.Add("ip", ServerAddress);
                data.Add("port", validPort);
                data.Add("path", "/tictactoe");
            }
            else
            {
                data.Add("result", 0);
            }

            Send(data);
        }

        private int FindNextValidPort()
        {
            var tmp = new TcpListener(IPAddress.Loopback, 0);
            tmp.Start();
            int port = ((IPEndPoint)tmp.LocalEndpoint).Port;
            tmp.Stop();
            return port;
        }

        private void Send(JsonNode data)
        {
            SendTextAsync(data.ToString());
        }

    }
    
    enum ActionType
    {
        ActCreateMatch = 1,
        ActUpdateMatch,
    }

    enum ResultType
    {
        CreateMatchFail = 0,
        CreateMatchSuccess = 1,
        MatchStart = 1,
        MatchEnd = 3,
        UpdateMatch = 2,
        Error = 0,
    }
}