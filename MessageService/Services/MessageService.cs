using CommandService;
using Grpc.Core;
using MessageService;

namespace MessageService.Services;

public class MessageService : CommandService.CommandService.CommandServiceBase
{
    private readonly ILogger<MessageService> _logger;

    public MessageService(ILogger<MessageService> logger)
    {
        _logger = logger;
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
            Random = random.Next(0, 100000000),
            Timestamp = DateTime.UtcNow.Ticks
        }
        );
        return Task.CompletedTask;
    }
    
    
}