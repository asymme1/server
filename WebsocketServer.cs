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
        }

        protected override void OnConnecting(TcpSession session)
        {
            Console.WriteLine($"New WS connection received : {session.Id}");
        }

        protected override void OnConnected(TcpSession session)
        {
            Console.WriteLine($"New WS connection established : {session.Id}");
            base.OnConnected(session);
        }

        protected override void OnDisconnecting(TcpSession session)
        {
            Console.WriteLine($"Disconnecting session {session.Id}");
            base.OnDisconnecting(session);
        }
    }
}