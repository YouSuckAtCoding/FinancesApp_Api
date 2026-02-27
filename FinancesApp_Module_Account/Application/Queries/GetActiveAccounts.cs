using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Account.Domain;

namespace FinancesApp_Module_Account.Application.Queries;
public class GetActiveAccounts : IQuery<IReadOnlyList<Account>>
{}
