using FinancesApp_CQRS.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancesApp_Module_Account.Domain.Events;
public record AccountClosedEvent(Guid EventId,
                                  DateTimeOffset Timestamp,
                                  Guid AccountId,
                                  Guid UserId) : IDomainEvent
{
}

