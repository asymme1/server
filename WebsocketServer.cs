using System.Net;
using NetCoreServer;

namespace woke3
{
    public class WebsocketServer : WsServer
    {
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
            // connect to "ws://104.194.240.14:8080"
        }
    }
}