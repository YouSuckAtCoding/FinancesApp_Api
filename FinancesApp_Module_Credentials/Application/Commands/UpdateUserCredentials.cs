using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Credentials.Domain;

namespace FinancesApp_Module_Credentials.Application.Commands;
public record UpdateUserCredentials(Guid UserId, string NewPlainPassword) : ICommand<bool>;
