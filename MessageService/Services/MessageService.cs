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
    
    public static ConcurrentDictionary<string, DateTime> AuthorizationTokens = new ConcurrentDictionary<string, DateTime>();
    public static ConcurrentDictionary<Guid, IServerStreamWriter<Message>> ActiveMessagesStreams = new ConcurrentDictionary<Guid, IServerStreamWriter<Message>>();
    public static ConcurrentDictionary<Guid, IServerStreamWriter<DataMessage>> ActiveDataMessagesStreams = new ConcurrentDictionary<Guid, IServerStreamWriter<DataMessage>>();
    public static ConcurrentQueue<Message> Messages = new ConcurrentQueue<Message>();
    public static ConcurrentQueue<DataMessage> DataMessages = new ConcurrentQueue<DataMessage>();

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
        reply.Timestamp = TimeUtils.GetCurrentTimestampInMilliseconds();
        reply.Version = Consts.Version;
        reply.Update = Consts.Update;
        reply.Namespace = _appSettings.ServerNameSpace;
        return Task.FromResult(reply);
    }

    public override Task<AuthorizationReply> Authorization(AuthorizationRequest request, ServerCallContext context)
    {
        AuthorizationReply reply = new AuthorizationReply();
        reply.Random = request.Random;
        reply.Timestamp = TimeUtils.GetCurrentTimestampInMilliseconds();
        if (_appSettings.ServerPassword == null || _appSettings.ServerPassword.Trim().Equals(""))
        {
            reply.Status = (int)HttpStatusCode.Unused;
            reply.Message = "This server does not require authentication";
            return  Task.FromResult(reply);
        }

        string serverSign = SignUtils.AuthorizationPasswordSign(
            _appSettings.ServerPassword,
            TimeUtils.GetCurrentTimestampInMilliseconds()
        );
        if (!request.Password.Equals(serverSign))
        {
            reply.Status = (int)HttpStatusCode.Unauthorized;
            reply.Message = "Password is incorrect";
            return Task.FromResult(reply);
        }
        
        // string randomToken = RandomUtils.RandomToken();
        // bool result = AuthorizationTokens.TryAdd(randomToken, DateTime.Now);
        // if (!result)
        // {
        //     reply.Status = (int)HttpStatusCode.InternalServerError;
        //     reply.Message = "Server error";
        //     return Task.FromResult(reply);
        // }
        // reply.Status = (int)HttpStatusCode.OK;
        // reply.Arguments.Add(randomToken);
        // reply.Message = "Authorization successful";
        // return Task.FromResult(reply);
        
        string randomToken = RandomUtils.RandomToken();
        reply.Status = (int)HttpStatusCode.OK;
        reply.Arguments.Add(randomToken);
        reply.Message = "Authorization successful";
        return Task.FromResult(reply);
    }

    public override Task<KeepAlive> KeepAlivePacket(KeepAlive keepAlive, ServerCallContext context)
    {
        KeepAlive keepAliveResponse = new KeepAlive();
        keepAliveResponse.Random = keepAlive.Random;
        keepAliveResponse.Timestamp = TimeUtils.GetCurrentTimestampInMilliseconds();
        return Task.FromResult(keepAliveResponse);
    }

    public override Task<Empty> KeepAliveClientStream(IAsyncStreamReader<KeepAlive> requestStream, ServerCallContext context)
    {
        return Task.FromResult(new Empty());
    }

    public override Task KeepAliveServerStream(Empty request, IServerStreamWriter<KeepAlive> responseStream, ServerCallContext context)
    {
        try
        {
            Random random = new Random();
            responseStream.WriteAsync(new KeepAlive
                {
                    Random = RandomUtils.GetRandomNumber(),
                    Timestamp = TimeUtils.GetCurrentTimestampInMilliseconds()
                }
            );
            return Task.CompletedTask;
        }
        catch (TaskCanceledException ex)
        {
            return Task.CompletedTask;
        }
    }
    
    public override Task<CommandReply> Command(CommandRequest request, ServerCallContext context)
    {
        if (!(_appSettings.ServerPassword == null || _appSettings.ServerPassword.Trim().Equals("")))
        {
            var header = context.RequestHeaders.FirstOrDefault(h => h.Key.ToLower() == "Authorization".ToLower());
            if (!context.RequestHeaders.Any(h => h.Key.ToLower() == "Authorization".ToLower()) || header == null)
            {
                return Task.FromResult(new CommandReply
                {
                    Status = (int)HttpStatusCode.Unauthorized,
                    Message = "Authorization required",
                });
            }
            if (!SignUtils.AuthorizationTokenSign(
                    _appSettings.ServerPassword.Trim(),
                    TimeUtils.GetCurrentTimestampInMilliseconds()
                ).Equals(header.Value))
            {
                return Task.FromResult(new CommandReply
                {
                    Status = (int)HttpStatusCode.Forbidden,
                    Message = "Authorization failed",
                });
            }
        }
        CommandReply reply = new CommandReply();
        reply.Status = (int)HttpStatusCode.NoContent;
        reply.Message = "ERROR";
        return Task.FromResult(reply);
    }

    public override Task<DataCommandReply> DataCommand(DataCommandRequest request, ServerCallContext context)
    {
        if (!(_appSettings.ServerPassword == null || _appSettings.ServerPassword.Trim().Equals("")))
        {
            var header = context.RequestHeaders.FirstOrDefault(h => h.Key.ToLower() == "Authorization".ToLower());
            if (!context.RequestHeaders.Any(h => h.Key.ToLower() == "Authorization".ToLower()) || header == null)
            {
                return Task.FromResult(new DataCommandReply
                {
                    Status = (int)HttpStatusCode.Unauthorized,
                    Message = "Authorization required",
                });
            }
            if (!SignUtils.AuthorizationTokenSign(
                    _appSettings.ServerPassword.Trim(),
                    TimeUtils.GetCurrentTimestampInMilliseconds()
                ).Equals(header.Value))
            {
                return Task.FromResult(new DataCommandReply
                {
                    Status = (int)HttpStatusCode.Forbidden,
                    Message = "Authorization failed",
                });
            }
        }
        DataCommandReply reply = new DataCommandReply();
        reply.Status = (int)HttpStatusCode.NoContent;
        reply.Message = "ERROR";
        return Task.FromResult(reply);
    }

    public override Task CommandStream(IAsyncStreamReader<CommandRequest> requestStream, IServerStreamWriter<CommandReply> responseStream, ServerCallContext context)
    {
        // try
        // {
        //     
        // }
        // catch (TaskCanceledException ex)
        // {
        //     return;
        // }
        return base.CommandStream(requestStream, responseStream, context);
    }

    public override Task DataCommandStream(IAsyncStreamReader<DataCommandRequest> requestStream, IServerStreamWriter<DataCommandReply> responseStream, ServerCallContext context)
    {
        // try
        // {
        //     
        // }
        // catch (TaskCanceledException ex)
        // {
        //     return;
        // }
        return base.DataCommandStream(requestStream, responseStream, context);
    }

    public override Task<Result> MessagePacket(Message request, ServerCallContext context)
    {
        if (!(_appSettings.ServerPassword == null || _appSettings.ServerPassword.Trim().Equals("")))
        {
            var header = context.RequestHeaders.FirstOrDefault(h => h.Key.ToLower() == "Authorization".ToLower());
            if (!context.RequestHeaders.Any(h => h.Key.ToLower() == "Authorization".ToLower()) || header == null)
            {
                return Task.FromResult(new Result
                {
                    Status = (int)HttpStatusCode.Unauthorized,
                    Message = "Authorization required",
                });
            }
            // if (!AuthorizationTokens.ContainsKey(header.Value))
            // {
            //     return Task.FromResult(new Result
            //     {
            //         Status = (int)HttpStatusCode.Forbidden,
            //         Message = "Authorization failed",
            //     });
            // }
            // if (!AuthorizationTokens.ContainsKey(header.Value)){return;}
            if (!SignUtils.AuthorizationTokenSign(
                    _appSettings.ServerPassword.Trim(),
                    TimeUtils.GetCurrentTimestampInMilliseconds()
                ).Equals(header.Value))
            {
                return Task.FromResult(new Result
                {
                    Status = (int)HttpStatusCode.Forbidden,
                    Message = "Authorization failed",
                });
            }
        }
        foreach (var stream in ActiveMessagesStreams)
        {
            try
            {
                // await
                stream.Value.WriteAsync(request);
            }
            catch (Exception ex)
            {
                ActiveMessagesStreams.TryRemove(stream.Key, out _);
            }
        }
        // Messages.Enqueue(request);
        Result result = new Result();
        result.Status = (int)HttpStatusCode.OK;
        result.Message = "RECEIVED";
        return Task.FromResult(result);
    }

    public override Task<Result> DataMessagePacket(DataMessage request, ServerCallContext context)
    {
        if (!(_appSettings.ServerPassword == null || _appSettings.ServerPassword.Trim().Equals("")))
        {
            var header = context.RequestHeaders.FirstOrDefault(h => h.Key.ToLower() == "Authorization".ToLower());
            if (!context.RequestHeaders.Any(h => h.Key.ToLower() == "Authorization".ToLower()) || header == null)
            {
                return Task.FromResult(new Result
                {
                    Status = (int)HttpStatusCode.Unauthorized,
                    Message = "Authorization required",
                });
            }
            // if (!AuthorizationTokens.ContainsKey(header.Value))
            // {
            //     return Task.FromResult(new Result
            //     {
            //         Status = (int)HttpStatusCode.Forbidden,
            //         Message = "Authorization failed",
            //     });
            // }
            // if (!AuthorizationTokens.ContainsKey(header.Value)){return;}
            if (!SignUtils.AuthorizationTokenSign(
                    _appSettings.ServerPassword.Trim(),
                    TimeUtils.GetCurrentTimestampInMilliseconds()
                ).Equals(header.Value))
            {
                return Task.FromResult(new Result
                {
                    Status = (int)HttpStatusCode.Forbidden,
                    Message = "Authorization failed",
                });
            }
        }
        foreach (var stream in ActiveDataMessagesStreams)
        {
            try
            {
                // await
                stream.Value.WriteAsync(request);
            }
            catch (Exception ex)
            {
                ActiveDataMessagesStreams.TryRemove(stream.Key, out _);
            }
        }
        // DataMessages.Enqueue(request);
        Result result = new Result();
        result.Status = (int)HttpStatusCode.OK;
        result.Message = "RECEIVED";
        return Task.FromResult(result);
    }

    public override async Task MessageStream(Empty request, IServerStreamWriter<Message> responseStream, ServerCallContext context)
    {
        try
        {
            if (!(_appSettings.ServerPassword == null || _appSettings.ServerPassword.Trim().Equals("")))
            {
                var header = context.RequestHeaders.FirstOrDefault(h => h.Key.ToLower() == "Authorization".ToLower());
                if (!context.RequestHeaders.Any(h => h.Key.ToLower() == "Authorization".ToLower()) || header == null)
                {
                    return;
                }

                // if (!AuthorizationTokens.ContainsKey(header.Value)){return;}
                if (!SignUtils.AuthorizationTokenSign(
                        _appSettings.ServerPassword.Trim(),
                        TimeUtils.GetCurrentTimestampInMilliseconds()
                    ).Equals(header.Value))
                {
                    return;
                }
            }
            
            Guid streamId = Guid.NewGuid();
            ActiveMessagesStreams.TryAdd(streamId, responseStream);
            try
            {
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(100), context.CancellationToken).ConfigureAwait(false);
                }
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
            {
                return;
            }
            finally
            {
                ActiveMessagesStreams.TryRemove(streamId, out _);
            }
            // while (!context.CancellationToken.IsCancellationRequested)
            // {
            //     Message? message;
            //     if (Messages.TryDequeue(out message))
            //     {
            //         await responseStream.WriteAsync(message).ConfigureAwait(false);
            //     }
            //     else
            //     {
            //         await Task.Delay(TimeSpan.FromMilliseconds(100), context.CancellationToken).ConfigureAwait(false);
            //     }
            // }
        }
        catch (TaskCanceledException ex)
        {
            return;
        }
    }

    public override async Task DataMessageStream(Empty request, IServerStreamWriter<DataMessage> responseStream, ServerCallContext context)
    {
        try
        {
            if (!(_appSettings.ServerPassword == null || _appSettings.ServerPassword.Trim().Equals("")))
            {
                var header = context.RequestHeaders.FirstOrDefault(h => h.Key.ToLower() == "Authorization".ToLower());
                if (!context.RequestHeaders.Any(h => h.Key.ToLower() == "Authorization".ToLower()) || header == null)
                {
                    return;
                }

                // if (!AuthorizationTokens.ContainsKey(header.Value)){return;}
                if (!SignUtils.AuthorizationTokenSign(
                        _appSettings.ServerPassword.Trim(),
                        TimeUtils.GetCurrentTimestampInMilliseconds()
                    ).Equals(header.Value))
                {
                    return;
                }
            }
            
            Guid streamId = Guid.NewGuid();
            ActiveDataMessagesStreams.TryAdd(streamId, responseStream);
            try
            {
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(100), context.CancellationToken).ConfigureAwait(false);
                }
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
            {
                return;
            }
            finally
            {
                ActiveDataMessagesStreams.TryRemove(streamId, out _);
            }
            // while (!context.CancellationToken.IsCancellationRequested)
            // {
            //     DataMessage? dataMessage;
            //     if (DataMessages.TryDequeue(out dataMessage))
            //     {
            //         await responseStream.WriteAsync(dataMessage).ConfigureAwait(false);
            //     }
            //     else
            //     {
            //         await Task.Delay(TimeSpan.FromMilliseconds(100), context.CancellationToken).ConfigureAwait(false);
            //     }
            // }
        }
        catch (TaskCanceledException ex)
        {
            return;
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
        // try
        // {
        //     
        // }
        // catch (TaskCanceledException ex)
        // {
        //     return base.EventStream(request, responseStream, context);
        // }
        return base.EventStream(request, responseStream, context);
    }

    public override Task<SubscribeReply> Subscribe(SubscribeRequest request, ServerCallContext context)
    {
        // Todo
        return base.Subscribe(request, context);
    }
    
    
}