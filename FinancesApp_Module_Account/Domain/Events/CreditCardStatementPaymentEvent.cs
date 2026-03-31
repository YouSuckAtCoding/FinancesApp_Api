using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Account.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancesApp_Module_Account.Domain.Events;
public record CreditCardStatementPaymentEvent(Guid EventId,
                                              DateTimeOffset Timestamp,
                                              Guid AccountId,
                                              Guid UserId,
                                              Money Amount) : IDomainEvent
{
}
