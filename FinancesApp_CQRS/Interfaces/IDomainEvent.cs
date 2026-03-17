using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancesApp_CQRS.Interfaces;
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset Timestamp { get; }
}