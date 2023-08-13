using System.Data;

namespace XFrame.Sql
{
    public interface ISqlConnectionFactory
    {
        Task<IDbConnection> OpenConnectionAsync(
            string connectionString,
            CancellationToken cancellationToken);
    }
}
