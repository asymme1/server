using System.Net;
using NetCoreServer;

namespace woke3
{
    public class GameServer : TcpServer
    {
        public GameSession Session = new();
        public GameServer(IPAddress address, int port) : base(address, port) {}

        protected override TcpSession CreateSession()
        {
            return new GameConnection(this, Session);
        }
    }
}