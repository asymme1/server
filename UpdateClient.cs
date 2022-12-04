using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Newtonsoft.Json.Linq;

namespace woke3;

public class UpdateClient
{
    private readonly WebsocketServer _main;
    private ClientWebSocket _ws;
    private readonly string Url = "ws://104.194.240.16/ws/channels/";
    
    public UpdateClient(WebsocketServer main)
    {
        _main = main;
        _ws = new ClientWebSocket();
        _ws.ConnectAsync(new Uri(Url), CancellationToken.None);
        Console.WriteLine("Connected with update channel");
        Task.Run(() => Loop());
    }
    
    private bool IsConnected() => _ws.State == WebSocketState.Open;

    private async Task Loop()
    {
        while (IsConnected())
        {
            var buffer = await Receive();
            if (buffer == null)
            {
                Console.WriteLine("Disconnected from update channel.");
                await _ws.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, "Disconnected", CancellationToken.None);
                break;
            }
            string message = Encoding.UTF8.GetString(buffer[0..^1]);
            Console.WriteLine(message);
            int lastCurlyBrace = message.LastIndexOf('}');
            var tmp = buffer[0..(lastCurlyBrace + 1)];
            JsonObject? data = JsonSerializer.Deserialize<JsonObject>(tmp);
            if (data == null)
                Console.WriteLine("Failed to deserialize data.");
            else
            {
                if (data["action"] == null) continue;
                int.TryParse(data["action"]?.ToString(), out int action);
                if (action == (int) ActionType.ActUpdateMatch)
                {
                    int.TryParse(data["match"]?.ToString(), out int match);
                    var matchInfo = _main.RequestMatchInfo(match);
                    if (matchInfo == null) continue;
                    await SendMatchUpdate(match, matchInfo);
                }
            }
        }
    }
    
    public async Task SendStartMatchMessage(int matchId)
    {
        JsonObject data = new JsonObject();
        data.Add("result", (int) ResultType.MatchStart);
        data.Add("match", matchId);
        await Send(data.ToString());
    }
    
    public async Task SendEndMatchMessage(int matchId)
    {
        JsonObject data = new JsonObject();
        data.Add("result", (int) ResultType.MatchEnd);
        data.Add("match", matchId);
        await Send(data.ToString());
    }

    private async Task SendMatchUpdate(int matchId, JObject info)
    {
        var data = new JObject(info)
        {
            { "result", (int)ResultType.UpdateMatch },
            { "match", matchId }
        };
        await Send(data.ToString());
    } 

    private async Task Send(string message)
    {
        var data = Encoding.UTF8.GetBytes(message);
        await _ws.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private async Task<byte[]?> Receive()
    {
        var buffer = new byte[1024];
        var result = await _ws.ReceiveAsync(buffer, CancellationToken.None);
        return buffer[..result.Count];
    }
}