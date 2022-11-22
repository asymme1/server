using System.Net;
using woke3;

Console.WriteLine("Server starting...");
var server = new GameServer(IPAddress.Any, 9001);
server.Start();
Console.WriteLine($"Done! Up on port {9001}.");
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