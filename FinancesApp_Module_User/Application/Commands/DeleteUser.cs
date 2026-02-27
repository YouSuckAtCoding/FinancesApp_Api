using FinancesApp_CQRS.Interfaces;

namespace FinancesApp_Module_User.Application.Commands;

public record DeleteUser(Guid UserId) : ICommand<bool>;