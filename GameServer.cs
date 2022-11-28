using System.Net;
using NetCoreServer;

namespace woke3
{
    public class GameServer : TcpServer
    {
        public GameSession Session = new();
        public WebsocketServer WsServer;

        public GameServer(IPAddress address, int port, int wsPort) : base(address, port)
        {
            WsServer = new WebsocketServer(address, wsPort);
        }

        protected override void OnStarted()
        {
            if (!WsServer.IsStarted)
            {
                WsServer.Start();
            }
        }

        protected override TcpSession CreateSession()
        {
            return new GameConnection(this, Session, WsServer);
        }
    }
}