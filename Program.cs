using System.Net;
using System.Net.WebSockets;
using System.Text;
using woke3;
using WebSocket = NetCoreServer.WebSocket;

var SERVER_PORT = 9000;
var mainServer = new WebsocketServer(IPAddress.Any, SERVER_PORT);
mainServer.Start();
await Task.Delay(-1);

enum PacketType
{
    PKT_SPECIAL_RESET = -1,
    PKT_HI = 0,
    PKT_ID,
    PKT_BOARD,
    PKT_SEND,
    PKT_RECEIVE,
    PKT_ERROR,
    PKT_END
}