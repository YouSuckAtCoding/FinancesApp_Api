using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_User.Application.Repositories;
using FinancesApp_Module_User.Domain;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace FinancesApp_Module_User.Application.Queries.Handlers;
public class GetUserByIdHandler(IEventStore eventStore,
                                IUserReadRepository userReadRepository,
                                ILogger<GetUserByIdHandler> logger) : IQueryHandler<GetUserById, User>
{
    private readonly IEventStore _eventStore = eventStore;
    private readonly IUserReadRepository _userReadRepository = userReadRepository;
    private readonly ILogger<GetUserByIdHandler> _logger = logger;

    private static readonly Counter GetUserByIdCounter = Metrics
        .CreateCounter("user_total_GetById", "Total number of users retrieved by id.");

    private static readonly Histogram GetUserByIdDuration = Metrics
        .CreateHistogram("user_GetById_duration_seconds", "User retrieved by id duration.",
            new HistogramConfiguration
            {
                Buckets = Histogram.LinearBuckets(start: 0.1, width: 0.1, count: 10)
            });

    public async Task<User> Handle(GetUserById query,
                                   CancellationToken token = default)
    {
        using (GetUserByIdDuration.NewTimer())
        {
            try
            {
                var events = await _eventStore.Load(query.UserId, token: token);

                GetUserByIdCounter.Inc();

                if (events.Count == 0)
                    return await _userReadRepository.GetUserById(query.UserId, token: token);

                var user = new User();
                user.RebuildFromEvents(events);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user with ID {UserId}", query.UserId);
                return new User();
            }
        }
    }
}
