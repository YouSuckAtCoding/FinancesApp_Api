using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Account.Domain;

namespace FinancesApp_Module_Account.Application.Commands;
public class ApplyDelta : ICommand<bool>
{
    public required Account Account { get; set; }
    public required OperationType OperationType { get; set; }
    public required string Currency { get; set; }
    public required decimal Value { get; set; }
    public required DateTimeOffset RequestedAt { get; set; }
}
