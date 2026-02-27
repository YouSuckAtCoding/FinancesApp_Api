using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_User.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancesApp_Module_User.Application.Queries;
public class GetUserById : IQuery<User>
{
    public Guid UserId { get; set; }
}
