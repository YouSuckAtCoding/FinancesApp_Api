using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Account.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancesApp_CQRS.Queries;
public class GetAccounts : IQuery<IReadOnlyList<Account>>
{}
