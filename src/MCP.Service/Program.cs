var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
var app = builder.Build();

app.MapControllers(); // This picks up [Route("rpc")] on ToolsController

app.Run("http://0.0.0.0:5050"); // Listen on all interfaces port 5000