using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_User.Application.Repositories;
using FinancesApp_Module_User.Domain;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace FinancesApp_Module_User.Application.Queries.Handlers;
public class GetUsersHandler(IUserReadRepository userReadRepository,
                             ILogger<GetUsersHandler> logger) : IQueryHandler<GetUsers, IReadOnlyList<User>>
{
    private readonly IUserReadRepository _userReadRepository = userReadRepository;
    private readonly ILogger<GetUsersHandler> _logger = logger;

    private static readonly Counter GetUsersCounter = Metrics
        .CreateCounter("user_total_GetAll", "Total number of get all users queries.");

    private static readonly Histogram GetUsersDuration = Metrics
        .CreateHistogram("user_GetAll_duration_seconds", "Get all users query duration.",
            new HistogramConfiguration
            {
                Buckets = Histogram.LinearBuckets(start: 0.1, width: 0.1, count: 10)
            });

    public async Task<IReadOnlyList<User>> Handle(GetUsers query,
                                                  CancellationToken token = default)
    {
        using (GetUsersDuration.NewTimer())
        {
            try
            {
                var result = await _userReadRepository.GetUsers(token: token);

                GetUsersCounter.Inc();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return [];
            }
        }
    }
}
