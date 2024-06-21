using CommandService;
using Grpc.Core;
using MessageService;
using MessageService.Utils;
using Microsoft.Extensions.Options;

namespace MessageService.Services;

public class MessageService : CommandService.CommandService.CommandServiceBase
{
    private readonly ILogger<MessageService> _logger;
    private readonly Settings _appSettings;

    public MessageService(ILogger<MessageService> logger, IOptionsMonitor<Settings> appSettings)
    {
        _logger = logger;
        _appSettings = appSettings.CurrentValue;
    }

    // public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    // {
    //     return Task.FromResult(new HelloReply
    //     {
    //         Message = "Hello " + request.Name
    //     });
    // }
    
    public override Task<Empty> EmptyPacket(Empty empty, ServerCallContext context)
    {
        return Task.FromResult(new Empty());
    }

    public override Task<HelloReply> Hello(HelloRequest request, ServerCallContext context)
    {
        HelloReply reply = new HelloReply();
        // reply.Random = request.Random;
        reply.Random = RandomUtils.GetRandomNumber();
        reply.Timestamp = DateTime.UtcNow.Ticks;
        reply.Version = Consts.Version;
        reply.Update = Consts.Update;
        reply.Namespace = _appSettings.ServerNameSpace;
        return Task.FromResult(reply);
    }

    public override Task<KeepAlive> KeepAlivePacket(KeepAlive keepAlive, ServerCallContext context)
    {
        KeepAlive keepAliveResponse = new KeepAlive();
        keepAliveResponse.Random = keepAlive.Random;
        keepAliveResponse.Timestamp = DateTime.UtcNow.Ticks;
        return Task.FromResult(keepAliveResponse);
    }

    public override Task<Empty> KeepAliveClientStream(IAsyncStreamReader<KeepAlive> requestStream, ServerCallContext context)
    {
        return  Task.FromResult(new Empty());
    }

    public override Task KeepAliveServerStream(Empty request, IServerStreamWriter<KeepAlive> responseStream, ServerCallContext context)
    {
        Random random = new Random();
        responseStream.WriteAsync( new KeepAlive
        {
            Random = RandomUtils.GetRandomNumber(),
            Timestamp = DateTime.UtcNow.Ticks
        }
        );
        return Task.CompletedTask;
    }
    
    
}