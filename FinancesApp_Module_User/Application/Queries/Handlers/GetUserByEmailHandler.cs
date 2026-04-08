using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_User.Application.Repositories;
using FinancesApp_Module_User.Domain;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace FinancesApp_Module_User.Application.Queries.Handlers;
public class GetUserByEmailHandler(IUserReadRepository userReadRepository,
                                ILogger<GetUserByEmailHandler> logger) : IQueryHandler<GetUserByEmail, User>
{
    private readonly IUserReadRepository _userReadRepository = userReadRepository;
    private readonly ILogger<GetUserByEmailHandler> _logger = logger;

    private static readonly Counter GetUserByEmailCounter = Metrics
        .CreateCounter("user_total_GetByEmail", "Total number of users retrieved by email.");

    private static readonly Histogram GetUserByEmailDuration = Metrics
        .CreateHistogram("user_GetByEmail_duration_seconds", "User retrieved by email duration.",
            new HistogramConfiguration
            {
                Buckets = Histogram.LinearBuckets(start: 0.1, width: 0.1, count: 10)
            });

    public async Task<User> Handle(GetUserByEmail query,
                                   CancellationToken token = default)
    {
        using (GetUserByEmailDuration.NewTimer())
        {
            try
            {
                var result = await _userReadRepository.GetUserByEmail(query.Email, token: token);

                GetUserByEmailCounter.Inc();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user with Email {Email}", query.Email);
                return new User();
            }
        }
    }
}
