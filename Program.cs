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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseWebSockets();

List<WebSocket> connections = new List<WebSocket>();

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


        Console.WriteLine($"Got request, body: \n {body}");
        WebSocket ws = await context.WebSockets.AcceptWebSocketAsync();
        
        connections.Add(ws);
        string message = body.Length > 0 ? body : "The body was empty";

        await Broadcast($"Connection started on the {connections.Count}th ws");
        await ReceiveMessage(ws, async (result, buffer) =>
        {
            if (result.MessageType == WebSocketMessageType.Text)
            {
                string text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                await Broadcast(text);
            }
            else if (result.MessageType == WebSocketMessageType.Close || ws.State == WebSocketState.Aborted)
            {
                connections.Remove(ws);
                await Broadcast("Disconnecting from ws connection");
                await ws.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
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
    Console.WriteLine(message);
    byte[] bytes = Encoding.UTF8.GetBytes(message);
    foreach (var socket in connections)
    {
        if (socket.State == WebSocketState.Open)
        {
            await socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}

app.Run();