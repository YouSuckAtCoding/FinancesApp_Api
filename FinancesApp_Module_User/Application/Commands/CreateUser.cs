using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_User.Domain;

namespace FinancesApp_Module_User.Application.Commands;

public record CreateUser(User User) : ICommand<Guid>;
