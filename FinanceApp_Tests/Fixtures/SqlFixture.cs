using FinanceAppDatabase.DbConnection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Testcontainers.MsSql;

namespace FinancesApp_Tests.Fixtures;
public class SqlFixture : IAsyncLifetime
{
    private MsSqlContainer _msSqlContainer = null!;
    public IDbConnectionFactory ConnectionFactory { get; private set; } = null!;
    public ICommandFactory CommandFactory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _msSqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

        await _msSqlContainer.StartAsync();
       
        ConnectionFactory = new DbConnectionFactory(_msSqlContainer.GetConnectionString());
        CommandFactory = new CommandFactory(ConnectionFactory);

        var init = new DatabaseInitializer(ConnectionFactory);
        await init.InitializeAsync();

    }
    public async Task ResetDatabaseAsync()
    {

        var init = new DatabaseInitializer(ConnectionFactory);
        await init.InitializeAsync();
    }
    public Task DisposeAsync()
    {
        return _msSqlContainer.DisposeAsync().AsTask();
    }
}