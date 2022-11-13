using System.Net;
using woke3;

Console.WriteLine("Server starting...");
var server = new GameServer(IPAddress.Any, 9001);
server.Start();
Console.WriteLine($"Done! Up on port {9001}.");
await Task.Delay(-1);

enum PacketType
{
    PKT_HI = 0,
    PKT_ID,
    PKT_BOARD,
    PKT_SEND,
    PKT_ERROR,
    PKT_END
}