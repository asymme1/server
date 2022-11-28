using System.Net;
using woke3;

string WEB_IP = "104.194.240.16";
int WEB_PORT = 8881;

// Console.WriteLine("Server starting...");
// var server = new GameServer(IPAddress.Any, 9001);
// server.Start();
// Console.WriteLine($"Done! Up on port {9001}.");

var gameServer = new GameServer(IPAddress.Any, 9001, 9002);
Console.WriteLine("Connecting to web server...");
var client = new ServerSession(WEB_IP, WEB_PORT, gameServer);
client.Connect();
Console.WriteLine("Connected to web server!");
gameServer.Start();
Console.WriteLine("Started game server!");
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