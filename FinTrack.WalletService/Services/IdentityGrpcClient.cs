using FinTrack.Contracts;
using Grpc.Net.Client;

namespace FinTrack.WalletService.Services;

/// <summary>
/// Calls IdentityService via gRPC to get user info before creating a wallet.
/// gRPC uses binary Protobuf — significantly faster than a REST call for
/// internal service-to-service communication.
/// </summary>
public class IdentityGrpcClient
{
    private readonly IdentityGrpc.IdentityGrpcClient _client;
    private readonly ILogger<IdentityGrpcClient> _logger;

    public IdentityGrpcClient(IdentityGrpc.IdentityGrpcClient client,
                               ILogger<IdentityGrpcClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<GetUserReply?> GetUserAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var reply = await _client.GetUserAsync(
                new GetUserRequest { UserId = userId.ToString() },
                cancellationToken: ct);

            return reply.Found ? reply : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "gRPC call to IdentityService failed for user {UserId}", userId);
            return null;
        }
    }
}
