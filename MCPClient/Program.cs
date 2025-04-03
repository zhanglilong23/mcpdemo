using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Configuration;
using ModelContextProtocol.Protocol.Transport;
using OpenAI;

Console.WriteLine($"程序启动中，请稍后");

McpClientOptions options = new()
{
    ClientInfo = new() { Name = "Weather Client", Version = "1.0.0" }
};

//1：注册MCPServer，以项目中引用为例。

//stdio方式运行MCPServer
/*var config = new McpServerConfig
{
    Id = "weather",
    Name = "Weather MCP Server",
    TransportType = TransportTypes.StdIo,
    TransportOptions = new Dictionary<string, string>
    {
        //运行MCPServer
        ["command"] = "dotnet",
        ["arguments"] = "run --project ../../../../MCPServer --no-build",
    }
};*/

//SSE远程方式连接MCPWebAPI
var config = new McpServerConfig
{
    Id = "weather",
    Name = "Weather MCP Server",
    TransportType = TransportTypes.Sse,
    Location = "http://127.0.0.1:5251/sse",
};


using var factory =
    LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace));

//2：创建MCPClient
await using var mcpClient = await McpClientFactory.CreateAsync(config, options);


//3：发现MCPServer中的Tool
var mcpTools = await mcpClient.ListToolsAsync();
foreach (var tool in mcpTools)
{
    Console.WriteLine($"{tool.Name} ({tool.Description})");
}

Console.WriteLine("---------- Tools");
Console.WriteLine();


//4：注册大模型

//注册方式1，使用本地模型。以本地使用Ollama启动的千问32b模型为例
//var openClient = new OllamaChatClient(new Uri("http://localhost:11434/"), "qwq:32b");

//注册方式2，使用远程模型。以阿里云百炼平台为例
var oclinet = new OpenAIClient(new System.ClientModel.ApiKeyCredential("sk-09fe7c698fd04ff7a4f2601ad45242be"), new OpenAIClientOptions
{
    Endpoint = new Uri("https://dashscope.aliyuncs.com/compatible-mode/v1")
});
//模型名称
var openClient = new OpenAIChatClient(oclinet, "qwen-max");

//测试模型，使用流式输出。
var res = openClient.GetStreamingResponseAsync("你好");
await foreach (var message in res)
{
    Console.Write(message);
}
Console.WriteLine();

Console.WriteLine("-------------llm test");
Console.WriteLine();


//5：创建Chat客户端
var client = new ChatClientBuilder(openClient)
    //添加日志
    .UseLogging(factory)
    //向聊天客户端添加函数调用
    .UseFunctionInvocation()
    .Build();

//6：执行对话
var msg = "";

while (true)
{
    Console.WriteLine();
    Console.WriteLine("这里是天气服务，你想咨询哪里的天气？");
    msg = Console.ReadLine();

    if (msg == "exit")
    {
        Console.WriteLine("程序退出");
        return;
    }

    IList<ChatMessage> messages =
    [
        //为ai设定身份
        new(ChatRole.System, """
                             你是一个天气助理，在输出天气时，请以家长口吻叮嘱用户添衣、带伞等。
                             """),
        new(ChatRole.User, msg)
    ];

    //区别于GetStreamingResponseAsync，此处示例非流式输出
    //注意，某些大模型要求流水输出，只能使用GetStreamingResponseAsync方式。
    var response =
    await client.GetResponseAsync(
        messages,
        new ChatOptions { Tools = [.. mcpTools] });

    Console.WriteLine(response);

}