using FinancesApp_CQRS.Interfaces;

namespace FinancesApp_Module_Credentials.Application.Commands;
public record RebuildCredentialsProjection(Guid UserId) : ICommand<bool>;
