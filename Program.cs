using Chat_WebSocket_Server;
using System.Net;
using System.Net.WebSockets;
using System.Text;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
const int PORT = 1234;
builder.WebHost.UseUrls($"https://localohost:{PORT}");
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

WebApplication app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseCors();

app.UseWebSockets();

List<WebSocket> connections = new List<WebSocket>();
List<Message> messages = new List<Message>();

string JSonifyMessages()
{
    string[] jsonMessages = messages.Select(message => message.ToJson()).ToArray();
    return $"[{string.Join(", ", jsonMessages)}]";
}

string toCounting(int number)
{
    switch (number % 10)
    {
        case 1:
            return number + "st";
        case 2:
            return number + "nd";
        default:
            break;
    }

    return number + "th";
}

app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        // get request body
        StreamReader bodyStreamReader = new StreamReader(context.Request.Body);
        int charsInBody = (int)Math.Min(context.Request.ContentLength ?? 0, int.MaxValue);
        char[] buffer = new char[charsInBody];
        await bodyStreamReader.ReadAsync(buffer, 0, charsInBody);
        string body = new string(buffer);

        WebSocket ws = await context.WebSockets.AcceptWebSocketAsync();
        
        connections.Add(ws);
        string connectionMessage = $"Connection started on the {toCounting(connections.Count)} web socket";
        Console.WriteLine(connectionMessage);
        await Broadcast(connectionMessage);

        await ReceiveMessage(ws, async (result, buffer) =>
        {
            if (result.MessageType == WebSocketMessageType.Text)
            {
                string text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                if (Message.isMessage(text))
                {
                    messages.Add(text);

                    await Broadcast(JSonifyMessages());
                } else
                {
                    // if it's not a message, just broadcast the same message like an echo
                    await Broadcast(text);
                }
            }
            else if (result.MessageType == WebSocketMessageType.Close || ws.State == WebSocketState.Aborted)
            {
                if (connections.Count > 0)
                {
                    connections.Remove(ws);
                    await Broadcast("Disconnecting from ws connection");
                    await ws.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                }
            }
        });
    }
    else
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
    }
});

async Task ReceiveMessage(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
{
    byte[] buffer = new byte[1024 * 4];
    while (socket.State == WebSocketState.Open)
    {
        WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        handleMessage(result, buffer);
    }
}

async Task Broadcast(string message)
{
    byte[] bytes = Encoding.UTF8.GetBytes(message);
    foreach (var socket in connections)
    {
        if (socket.State == WebSocketState.Open)
        {
            await socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}

app.MapGet("/messages", () => JSonifyMessages());


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();