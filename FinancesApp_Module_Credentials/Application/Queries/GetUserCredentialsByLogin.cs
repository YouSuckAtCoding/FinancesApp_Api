using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Credentials.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancesApp_Module_Credentials.Application.Queries;
public class GetUserCredentialsByLogin : IQuery<UserCredentials>
{
    public string Login { get; set; } = "";
    public string Password { get; set; } = "";
}
