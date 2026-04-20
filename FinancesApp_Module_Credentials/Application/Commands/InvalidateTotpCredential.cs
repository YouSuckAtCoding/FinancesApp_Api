using FinancesApp_CQRS.Interfaces;

namespace FinancesApp_Module_Credentials.Application.Commands;
public record InvalidateTotpCredential(Guid TotpId) : ICommand<bool>;
