using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using NetCoreServer;
using Newtonsoft.Json.Linq;

namespace woke3
{
    public class WebsocketSession : WsSession
    {
        private readonly WebsocketServer _server;

        public WebsocketSession(WebsocketServer server) : base(server)
        {
            _server = server;
        }

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
            int lastCurlyBrace = message.LastIndexOf('}');
            var tmp = buffer[0..(lastCurlyBrace + 1)];
            var tmp2 = Encoding.ASCII.GetString(buffer);
            JsonObject? data = JsonSerializer.Deserialize<JsonObject>(tmp, option);
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
                    case ActionType.ActRequestMatchs:
                        Console.WriteLine("UI Client requested matches");
                        SendAllMatches();
                        break;
                    case ActionType.ActRequestMatchInfo:
                        int.TryParse(data["match"]?.ToString(), out matchId);
                        SendMatchInfoToUiClient(matchId);
                        break;
                }
            }
        }

        public JObject? RequestMatchInfo(int matchId)
        {
            for (int i = 0; i < _server.GameSessions.Count; i++)
                if (_server.GameSessions[i].MatchId == matchId)
                    return _server.GameSessions[i].GetInfo();
            
            return null;
        }
        
        /// <summary>
        /// Send all matches to the ui client
        /// </summary>
        private void SendAllMatches()
        {
            JObject data = new JObject();
            var matches = new JArray();
            foreach (var s in _server.GameSessions)
            {
                matches.Add(new JObject()
                {
                    { "matchId", s.MatchId },
                    { "port", s.Port },
                    { "uid1", s.Uid1 },
                    { "uid2", s.Uid2 },
                    { "state", (int) s.MatchState },
                    { "col", s.Matrix.Length / s.Matrix.GetLength(0) },
                    { "row", s.Matrix.GetLength(0) },
                });
            }
            Console.WriteLine(matches.ToString());
            data.Add("matches", matches);
            Console.WriteLine(data.ToString());
            Send(data);
        }
        
        private void SendMatchInfoToUiClient(int matchId)
        {
            JObject data = new JObject();
            var board = new JArray();
            var gameSession = _server.GameSessions.Find(s => s.MatchId == matchId);
            var matrix = gameSession?.Matrix;
            for (int i = 0; i < matrix?.GetLength(0); ++i)
            {
                var row = new JArray();
                for (int j = 0; j < matrix?.Length/ matrix?.GetLength(0); ++j)
                {
                    var cell = matrix?[i, j];
                    if (cell == gameSession?.P1) cell = 1;
                    else if (cell == gameSession?.P2) cell = 2;
                    row.Add(cell);
                }
                board.Add(row);
            }
            data.Add("board", board);
            data.Add("first", gameSession?.RegisteredUid);
            Send(data);
        }

        private void CreateAndSendCreateState(int matchId, int uid1, int uid2, string password)
        {
            int validPort = FindNextValidPort();
            var gameServer = new GameServer(IPAddress.Any, validPort);
            gameServer.Start();
            Console.WriteLine($"New game server started on port {validPort}");
            bool state = gameServer.CreateMatch(matchId, uid1, uid2, password);
            gameServer.Session.MainServer = _server;
            gameServer.Session.Port = validPort;
            _server.GameSessions.Add(gameServer.Session);

            JObject data = new JObject();
            if (state)
            {
                data.Add("result", (int) ResultType.CreateMatchSuccess);
                data.Add("ip", WebsocketServer.ServerAddress);
                data.Add("port", validPort);
                data.Add("path", "/tictactoe");
            }
            else
            {
                data.Add("result", (int) ResultType.CreateMatchFail);
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

        private void Send(JObject data)
        {
            SendTextAsync(data.ToString());
        }
    }
    
    enum ActionType
    {
        ActCreateMatch = 1,
        ActUpdateMatch = 2,
        ActRequestMatchs = 8,
        ActRequestMatchInfo = 9,
    }

    public enum ResultType
    {
        CreateMatchFail = 0,
        CreateMatchSuccess = 1,
        MatchStart = 1,
        MatchEnd = 3,
        UpdateMatch = 2,
        Error = 0,
    }
}