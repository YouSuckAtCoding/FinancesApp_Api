using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Account.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancesApp_Module_Account.Domain.Events;
public record AccountCreatedEvent(Guid EventId,
                                  DateTimeOffset Timestamp,
                                  Guid Id,
                                  Guid userId,
                                  Money balance,
                                  Money debt,
                                  AccountType type) : IDomainEvent
{
}
