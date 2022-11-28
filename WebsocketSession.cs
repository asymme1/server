using NetCoreServer;

namespace woke3
{
    public class WebsocketSession : WsSession
    {
        public WebsocketSession(WsServer server) : base(server) {}

        protected override void OnConnected()
        {
            SendText("welcome");
            base.OnConnected();
        }

        public override void OnWsConnected(HttpResponse response)
        {
            SendText("welcome");
            base.OnWsConnected(response);
        }
    }
}