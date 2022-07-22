using Grpc.Core;

namespace ChallengeAssistant.Services.Contracts;

public class GrpcChallengeService : ChallengeAssistant.ChallengeService.ChallengeServiceBase
{
    private readonly ILogger<GrpcChallengeService> _logger;
    
    public GrpcChallengeService(ILogger<GrpcChallengeService> logger)
    {
        _logger = logger;
    }

    public override Task GetChallenges(EmptyRequest request, IServerStreamWriter<Challenge> responseStream, ServerCallContext context)
    {
        return base.GetChallenges(request, responseStream, context);
    }

    public override Task<ChallengeReportResponse> SubmitChallenge(SubmitChallengeRequest request, ServerCallContext context)
    {
        return base.SubmitChallenge(request, context);
    }
}