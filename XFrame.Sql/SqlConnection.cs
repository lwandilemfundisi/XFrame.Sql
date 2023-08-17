using Dapper;
using Microsoft.Extensions.Logging;
using System.Data;
using XFrame.Common;
using XFrame.Resilience;

namespace XFrame.Sql
{
    public abstract class SqlConnection<TConfiguration, TRetryStrategy, TConnectionFactory> : ISqlConnection
        where TConfiguration : ISqlConfiguration<TConfiguration>
        where TRetryStrategy : IResilientStrategy
        where TConnectionFactory : ISqlConnectionFactory
    {
        protected SqlConnection(
            ILogger logger,
            TConfiguration configuration,
            TConnectionFactory connectionFactory,
            ITransientFaultHandler<TRetryStrategy> transientFaultHandler)
        {
            Logger = logger;
            ConnectionFactory = connectionFactory;
            Configuration = configuration;
            TransientFaultHandler = transientFaultHandler;
        }

        public ILogger Logger { get; }
        protected TConnectionFactory ConnectionFactory { get; }
        protected TConfiguration Configuration { get; }
        protected ITransientFaultHandler<TRetryStrategy> TransientFaultHandler { get; }

        public virtual Task<int> ExecuteAsync(
            Label label,
            string connectionStringName,
            CancellationToken cancellationToken,
            string sql,
            object? param = null)
        {
            return WithConnectionAsync(
                label,
                connectionStringName,
                (c, ct) =>
                {
                    var commandDefinition = new CommandDefinition(sql, param, cancellationToken: ct);
                    return c.ExecuteAsync(commandDefinition);
                },
                cancellationToken);
        }

        public virtual async Task<IReadOnlyCollection<TResult>> QueryAsync<TResult>(
            Label label,
            string connectionStringName,
            CancellationToken cancellationToken,
            string sql,
            object? param = null)
        {
            return (
                await WithConnectionAsync(
                    label,
                    connectionStringName,
                    (c, ct) =>
                    {
                        var commandDefinition = new CommandDefinition(sql, param, cancellationToken: ct);
                        return c.QueryAsync<TResult>(commandDefinition);
                    },
                    cancellationToken)
                    .ConfigureAwait(false))
                .ToList();
        }

        public virtual Task<IReadOnlyCollection<TResult>> InsertMultipleAsync<TResult, TRow>(
            Label label,
            string connectionStringName,
            CancellationToken cancellationToken,
            string sql,
            IEnumerable<TRow> rows)
            where TRow : class
        {
            Logger.LogDebug("Insert multiple rows non-optimized, inserting one row at a time using SQL {SQL}", sql);

            return WithConnectionAsync<IReadOnlyCollection<TResult>>(
                label,
                connectionStringName,
                async (c, ct) =>
                {
                    using (var transaction = c.BeginTransaction())
                    {
                        try
                        {
                            var results = new List<TResult>();
                            foreach (var row in rows)
                            {
                                var commandDefinition = new CommandDefinition(sql, row, cancellationToken: ct);
                                var result = await c.QueryAsync<TResult>(commandDefinition).ConfigureAwait(false);
                                results.Add(result.First());
                            }
                            transaction.Commit();
                            return results;
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                },
                cancellationToken);
        }

        public virtual Task<TResult> WithConnectionAsync<TResult>(
            Label label,
            string connectionStringName,
            Func<IDbConnection, CancellationToken, Task<TResult>> withConnection,
            CancellationToken cancellationToken)
        {
            return TransientFaultHandler.TryAsync(
                async c =>
                {
                    var connectionString = await Configuration.GetConnectionStringAsync(
                        label,
                        connectionStringName,
                        cancellationToken).ConfigureAwait(false);

                    using (var sqlConnection = await ConnectionFactory
                    .OpenConnectionAsync(connectionString, cancellationToken).ConfigureAwait(false))
                    {
                        return await withConnection(sqlConnection, c).ConfigureAwait(false);
                    }
                },
                label,
                cancellationToken);
        }
    }
}
