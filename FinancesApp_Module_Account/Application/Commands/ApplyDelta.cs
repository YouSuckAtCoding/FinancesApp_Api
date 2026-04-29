using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Account.Domain;
using FinancesApp_Module_Account.Domain.ValueObjects;

namespace FinancesApp_Module_Account.Application.Commands;
public class ApplyDelta : ICommand<ApplyDeltaResult>
{
    public required Account Account { get; set; }
    public required OperationType OperationType { get; set; }
    public required Money Delta { get; set; }
    public required DateTimeOffset RequestedAt { get; set; }
}
