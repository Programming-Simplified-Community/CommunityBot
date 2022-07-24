using Dummy;
using Grpc.Core;

namespace ChallengeAssistant.Grpc;

public class DummyImpl : DummyService.DummyServiceBase
{
    private readonly ILogger<DummyImpl> _logger;

    public DummyImpl(ILogger<DummyImpl> logger)
    {
        _logger = logger;
    }

    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        return Task.FromResult(new HelloReply
        {
            Message = $"Hello: {request.Name}"
        });
    }
}