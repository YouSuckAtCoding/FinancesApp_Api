using Microsoft.Data.SqlClient;

namespace FinanceAppDatabase.DbConnection;
public class CommandFactory : ICommandFactory
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CommandFactory(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Method for queries with a single connection (return needed)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="commandText">Whatever command or procedure you want. Make sure to change the CreateSqlCommandOptions.commandType to match your text</param>
    /// <param name="options">Base class of SqlCommandOptions. Only instantiate new if needed</param>
    /// <param name="operation">The delegate operation you want from the command.</param>
    /// <param name="token">Cancellation Token</param>
    /// <returns></returns>
    public async Task<T> ExecuteAsync<T>(
        string commandText,
        CreateSqlCommandOptions options,
        Func<SqlCommand, Task<T>> operation,
        CancellationToken token = default)
    {

        using var connection = await _connectionFactory.GetConnection(token);
        using var command = connection.CreateCommand();

        command.CommandText = commandText;
        command.CommandType = options.commandType;

        command.Parameters.AddRange(options.Parameters);

        return await operation(command);

    }

    /// <summary>
    /// Method for scalar operations with a single connection (No return needed)
    /// </summary>
    /// <param name="commandText">Whatever command or procedure you want. Make sure to change the CreateSqlCommandOptions.commandType to match your text</param>
    /// <param name="options">Base class of SqlCommandOptions. Only instantiate new if needed</param>
    /// <param name="operation">The delegate operation you want from the command.</param>
    /// <param name="token">Cancellation Token</param>
    /// <returns></returns>
    public async Task ExecuteAsync(
        string commandText,
        CreateSqlCommandOptions options,
        Func<SqlCommand, Task> operation,
        CancellationToken token = default)
    {
        using var connection = await _connectionFactory.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = commandText;

        command.CommandType = options.commandType;

        command.Parameters.AddRange(options.Parameters);

        await operation(command);
    }


    /// <summary>
    /// Method for query operations with a reusable connection (With return)
    /// </summary>
    /// <param name="connection">SqlConnection to be reused across workflow</param>
    /// <param name="commandText">Whatever command or procedure you want. Make sure to change the CreateSqlCommandOptions.commandType to match your text</param>
    /// <param name="options">Base class of SqlCommandOptions. Only instantiate new if needed</param>
    /// <param name="operation">The delegate operation you want from the command.</param>
    /// <param name="token">Cancellation Token</param>
    /// <returns></returns>
    public async Task<T> ExecuteAsync<T>(
        string commandText,
        SqlConnection? connection,
        CreateSqlCommandOptions options,
        Func<SqlCommand, Task<T>> operation,
        CancellationToken token = default)
    {
        connection ??= await _connectionFactory.GetConnection(token);

        using var command = connection.CreateCommand();

        command.CommandText = commandText;
        command.CommandType = options.commandType;
        command.Parameters.AddRange(options.Parameters);

        return await operation(command);
    }
    /// <summary>
    /// Method for scalar operations with a reusable connection (No return needed)
    /// </summary>
    /// <param name="connection">SqlConnection to be reused across workflow</param>
    /// <param name="commandText">Whatever command or procedure you want. Make sure to change the CreateSqlCommandOptions.commandType to match your text</param>
    /// <param name="options">Base class of SqlCommandOptions. Only instantiate new if needed</param>
    /// <param name="operation">The delegate operation you want from the command.</param>
    /// <param name="token">Cancellation Token</param>
    /// <returns></returns>
    public async Task ExecuteAsync(
        string commandText,
        SqlConnection? connection,
        CreateSqlCommandOptions options,
        Func<SqlCommand, Task> operation,
        CancellationToken token = default)
    {
        connection ??= await _connectionFactory.GetConnection(token);
        using var command = connection.CreateCommand();

        command.CommandText = commandText;

        command.CommandType = options.commandType;

        command.Parameters.AddRange(options.Parameters);

        await operation(command);
    }
    /// <summary>
    /// Method for query operations with a reusable connection (With return)
    /// </summary>
    /// <param name="connection">SqlConnection to be reused across workflow</param>
    /// <param name="commandText">Whatever command or procedure you want. Make sure to change the CreateSqlCommandOptions.commandType to match your text</param>
    /// <param name="options">Base class of SqlCommandOptions. Only instantiate new if needed</param>
    /// <param name="operation">The delegate operation you want from the command.</param>
    /// <param name="token">Cancellation Token</param>
    /// <returns></returns>
    public async Task<T> ExecuteAsync<T>(
        string commandText,
        SqlConnection? connection,
        SqlTransaction transaction,
        CreateSqlCommandOptions options,
        Func<SqlCommand, Task<T>> operation,
        CancellationToken token = default)
    {
        connection ??= await _connectionFactory.GetConnection(token);
        using var command = connection.CreateCommand();

        command.CommandText = commandText;

        command.CommandType = options.commandType;
        command.Transaction = transaction;

        command.Parameters.AddRange(options.Parameters);

        return await operation(command);
    }
    /// <summary>
    /// Method for scalar operations with a reusable connection (No return needed)
    /// </summary>
    /// <param name="connection">SqlConnection to be reused across workflow</param>
    /// <param name="commandText">Whatever command or procedure you want. Make sure to change the CreateSqlCommandOptions.commandType to match your text</param>
    /// <param name="options">Base class of SqlCommandOptions. Only instantiate new if needed</param>
    /// <param name="operation">The delegate operation you want from the command.</param>
    /// <param name="token">Cancellation Token</param>
    /// <returns></returns>
    public async Task ExecuteAsync(
        string commandText,
        SqlConnection? connection,
        SqlTransaction transaction,
        CreateSqlCommandOptions options,
        Func<SqlCommand, Task> operation,
        CancellationToken token = default)
    {
        connection ??= await _connectionFactory.GetConnection(token);
        using var command = connection.CreateCommand();

        command.CommandText = commandText;

        command.CommandType = options.commandType;
        command.Transaction = transaction;

        command.Parameters.AddRange(options.Parameters);

        await operation(command);
    }
}

public class CreateSqlCommandOptions
{
    public SqlParameter[] Parameters { get; set; } = [];
    public System.Data.CommandType commandType { get; set; } = System.Data.CommandType.Text;

}