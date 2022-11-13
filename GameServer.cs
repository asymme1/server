using System.Net;
using NetCoreServer;

namespace woke3
{
    public class GameServer : TcpServer
    {
        private readonly GameSession session = new();
        public GameServer(IPAddress address, int port) : base(address, port) {}

        protected override TcpSession CreateSession()
        {
            return new GameConnection(this, session);
        }
    }
}