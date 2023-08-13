using XFrame.Common;
using XFrame.Resilience;

namespace XFrame.Sql
{
    public interface ISqlConfiguration<out T>
        where T : ISqlConfiguration<T>
    {
        RepeatDelay TransientRepeatDelay { get; }
        int TransientRetryCount { get; }
        TimeSpan UpgradeExecutionTimeout { get; }
        T SetConnectionString(string connectionString);
        T SetConnectionString(string connectionStringName, string connectionString);
        T SetTransientRepeatDelay(RepeatDelay retryDelay);
        T SetTransientRetryCount(int retryCount);
        T SetUpgradeExecutionTimeout(TimeSpan timeout);
        Task<string> GetConnectionStringAsync(
            Label label,
            string name,
            CancellationToken cancellationToken);
    }
}
