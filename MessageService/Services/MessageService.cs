using System.Collections.Concurrent;
using System.Net;
using CommandService;
using Google.Protobuf.Collections;
using Grpc.Core;
using MessageService;
using MessageService.Utils;
using Microsoft.Extensions.Options;

namespace MessageService.Services;

public class MessageService : CommandService.CommandService.CommandServiceBase
{
    private readonly ILogger<MessageService> _logger;
    private readonly Settings _appSettings;
    
    public ConcurrentQueue<Message> Messages = new ConcurrentQueue<Message>();
    public ConcurrentQueue<DataMessage> DataMessages = new ConcurrentQueue<DataMessage>();

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
    
    public override Task<CommandReply> Command(CommandRequest request, ServerCallContext context)
    {
        CommandReply reply = new CommandReply();
        reply.Status = (int)HttpStatusCode.NoContent;
        reply.Message = "ERROR";
        return Task.FromResult(reply);
    }

    public override Task<DataCommandReply> DataCommand(DataCommandRequest request, ServerCallContext context)
    {
        DataCommandReply reply = new DataCommandReply();
        reply.Status = (int)HttpStatusCode.NoContent;
        reply.Message = "ERROR";
        return Task.FromResult(reply);
    }

    public override Task<Result> MessagePacket(Message request, ServerCallContext context)
    {
        Messages.Enqueue(request);
        Result result = new Result();
        result.Status = (int)HttpStatusCode.OK;
        result.Message = "RECEIVED";
        return Task.FromResult(result);
    }

    public override Task<Result> DataMessagePacket(DataMessage request, ServerCallContext context)
    {
        DataMessages.Enqueue(request);
        Result result = new Result();
        result.Status = (int)HttpStatusCode.OK;
        result.Message = "RECEIVED";
        return Task.FromResult(result);
    }

    public override async Task MessageStream(Empty request, IServerStreamWriter<Message> responseStream, ServerCallContext context)
    {
        while (!context.CancellationToken.IsCancellationRequested)
        {
            Message? message;
            if (Messages.TryDequeue(out message))
            {
                await responseStream.WriteAsync(message).ConfigureAwait(false);
            }
            else
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), context.CancellationToken).ConfigureAwait(false);
            }
        }
    }

    public override async Task DataMessageStream(Empty request, IServerStreamWriter<DataMessage> responseStream, ServerCallContext context)
    {
        while (!context.CancellationToken.IsCancellationRequested)
        {
            DataMessage? dataMessage;
            if (DataMessages.TryDequeue(out dataMessage))
            {
                await responseStream.WriteAsync(dataMessage).ConfigureAwait(false);
            }
            else
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), context.CancellationToken).ConfigureAwait(false);
            }
        }
    }

    public override Task<Result> Event(EventRequest request, ServerCallContext context)
    {
        // Todo
        return base.Event(request, context);
    }

    public override Task EventStream(Empty request, IServerStreamWriter<EventReply> responseStream, ServerCallContext context)
    {
        // Todo
        return base.EventStream(request, responseStream, context);
    }

    public override Task<SubscribeReply> Subscribe(SubscribeRequest request, ServerCallContext context)
    {
        // Todo
        return base.Subscribe(request, context);
    }
    
    
}