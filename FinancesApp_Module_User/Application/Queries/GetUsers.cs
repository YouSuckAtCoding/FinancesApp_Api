using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_User.Domain;

namespace FinancesApp_Module_User.Application.Queries;
public class GetUsers : IQuery<IReadOnlyList<User>>
{
}
