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
            return new GameConnection(this, WsServer);
        }

        public bool CreateMatch(int matchId, int uid1, int uid2, string keymatch)
        {
            Session = new GameSession(matchId, uid1, uid2, keymatch);
            return true;
        }
    }
}