using NetCoreServer;

namespace woke3
{
    public class WebsocketSession : WsSession
    {
        public WebsocketSession(WsServer server) : base(server) {}

        public override void OnWsConnected(HttpRequest req)
        {
            SendText("welcome");
            base.OnWsConnected(req);
        }
    }
}