using System.Net;
using NetCoreServer;
using Newtonsoft.Json.Linq;

namespace woke3
{
    public class WebsocketServer : WsServer
    {
        public static readonly bool IsDevServer = true;
        public static readonly string ServerAddress = IsDevServer ? "tcp://0.tcp.ap.ngrok.io" : "";
        public readonly List<GameSession> GameSessions = new List<GameSession>();
        public WebsocketServer(IPAddress address, int port) : base(address, port) {}

        protected override TcpSession CreateSession()
        {
            return new WebsocketSession(this);
        }

        protected override void OnStarting()
        {
            Console.WriteLine($"Starting websocket server on port {Port}...");
        }

        protected override void OnStarted()
        {
            Console.WriteLine($"Started websocket server on port {Port}...");
        }
        
        public JObject? RequestMatchInfo(int matchId)
        {
            for (int i = 0; i < GameSessions.Count; i++)
                if (GameSessions[i].MatchId == matchId)
                    return GameSessions[i].GetInfo();
            
            return null;
        }
    }
}