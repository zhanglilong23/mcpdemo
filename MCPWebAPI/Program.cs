using ModelContextProtocol.AspNetCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddOpenApi();

//ע��MCPServer�����ӵ�ǰ���򼯼���Tool
builder.Services.AddMcpServer().WithToolsFromAssembly();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

//ע��MCPServer��·��
app.MapMcp();

app.Run();
