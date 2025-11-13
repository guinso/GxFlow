var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

//app.UseWebSockets(); // Enable WebSockets middleware
//app.Map("/ws", async context => { 
//    if(context.WebSockets.IsWebSocketRequest)
//    {
//        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
//    }
//    else
//    {
//        context.Response.StatusCode = StatusCodes.Status400BadRequest;
//    }
//});

app.UseFileServer();

app.MapGet("/hello", () => "Hello World!");
app.Run();