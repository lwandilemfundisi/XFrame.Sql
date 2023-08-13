using System.Collections.Concurrent;
using XFrame.Common;
using XFrame.Resilience;

namespace XFrame.Sql
{
    public abstract class SqlConfiguration<T> : ISqlConfiguration<T>
        where T : ISqlConfiguration<T>
    {
        private readonly ConcurrentDictionary<string, string> _connectionStrings = 
            new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public RepeatDelay TransientRepeatDelay { get; private set; } = RepeatDelay.Between(
            TimeSpan.FromMilliseconds(50),
            TimeSpan.FromMilliseconds(100));

        public int TransientRetryCount { get; private set; } = 2;
        public TimeSpan UpgradeExecutionTimeout { get; private set; } = TimeSpan.FromMinutes(5);

        public T SetConnectionString(string connectionString)
        {
            if (!_connectionStrings.TryAdd(string.Empty, connectionString))
            {
                throw new ArgumentException("Default connection string already configured");
            }

            return (T)(object)this;
        }

        public T SetConnectionString(
            string connectionStringName,
            string connectionString)
        {
            if (!_connectionStrings.TryAdd(connectionStringName, connectionString))
            {
                throw new ArgumentException(
                    $"There's already a connection string named '{connectionStringName}'",
                    nameof(connectionStringName));
            }

            return (T)(object)this;
        }

        public T SetTransientRepeatDelay(RepeatDelay RepeatDelay)
        {
            TransientRepeatDelay = RepeatDelay;

            return (T)(object)this;
        }

        public T SetTransientRetryCount(int retryCount)
        {
            TransientRetryCount = retryCount;

            return (T)(object)this;
        }

        public T SetUpgradeExecutionTimeout(TimeSpan timeout)
        {
            UpgradeExecutionTimeout = timeout;

            return (T)(object)this;
        }

        public virtual Task<string> GetConnectionStringAsync(
            Label label,
            string name,
            CancellationToken cancellationToken)
        {
            if (!_connectionStrings.TryGetValue(name ?? string.Empty, out var connectionString))
            {
                throw new ArgumentOutOfRangeException($"There's no connection string named '{name}'");
            }

            return Task.FromResult(connectionString);
        }
    }
}
