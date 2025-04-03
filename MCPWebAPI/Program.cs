using ModelContextProtocol.AspNetCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddOpenApi();

//注册MCPServer，并从当前程序集加载Tool
builder.Services.AddMcpServer().WithToolsFromAssembly();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

//注册MCPServer的路由
app.MapMcp();

app.Run();
