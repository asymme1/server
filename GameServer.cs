using System.Net;
using NetCoreServer;

namespace woke3
{
    public class GameServer : TcpServer
    {
        public GameSession Session;
        public WebsocketServer WsServer;

        public GameServer(IPAddress address, int port, int wsPort) : base(address, port)
        {
            Session = new GameSession(1, 1, 2, "asasdas");
            WsServer = new WebsocketServer(address, wsPort);
            WsServer.Start();
        }

        protected override TcpSession CreateSession()
        {
            return new GameConnection(this, Session, WsServer);
        }

        public bool CreateMatch(int matchId, int uid1, int uid2, string keymatch)
        {
            Session = new GameSession(matchId, uid1, uid2, keymatch);
            return true;
        }
    }
}