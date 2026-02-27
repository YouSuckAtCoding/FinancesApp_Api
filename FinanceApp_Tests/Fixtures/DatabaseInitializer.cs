using FinanceAppDatabase.DbConnection;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancesApp_Tests.Fixtures;
public class DatabaseInitializer
{
    private IDbConnectionFactory _connectionFactory;

    public DatabaseInitializer(IDbConnectionFactory sqlAccessConnectionFactory)
    {
        _connectionFactory = sqlAccessConnectionFactory;
    }

    public async Task InitializeAsync()
    {
        using var connection = await _connectionFactory.GetConnection();
        
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        string script = File.ReadAllText("C:\\Projetos\\PersonalFinance\\Api\\FinanceApp_Tests\\FinanceAppDb_V6.sql");
        ExecuteScript(connection, script);
    }
    protected virtual void ExecuteScript(SqlConnection connection, string script)
    {
        try
        {
            string[] commandTextArray = System.Text.RegularExpressions.Regex.Split(script, "\r\n[\t ]*GO");

            SqlCommand _cmd = new SqlCommand(string.Empty, connection);

            foreach (string commandText in commandTextArray)
            {
                if (commandText.Trim() == string.Empty) continue;
                if ((commandText.Length >= 3) && (commandText.Substring(0, 3).ToUpper() == "USE"))
                {
                    throw new Exception("Create-script contains USE-statement. Please provide non-database specific create-scripts!");
                }

                _cmd.CommandText = commandText;
                _cmd.ExecuteNonQuery();
            }

        }
        catch
        {
            throw;
        }



    }
}
