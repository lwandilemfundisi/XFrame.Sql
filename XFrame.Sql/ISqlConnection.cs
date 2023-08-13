using System.Data;
using XFrame.Common;

namespace XFrame.Sql
{
    public interface ISqlConnection
    {
        Task<int> ExecuteAsync(
            Label label,
            string connectionStringName,
            CancellationToken cancellationToken,
            string sql,
            object? param = null);
        
        Task<IReadOnlyCollection<TResult>> QueryAsync<TResult>(
            Label label,
            string connectionStringName,
            CancellationToken cancellationToken,
            string sql,
            object? param = null);
        
        Task<IReadOnlyCollection<TResult>> InsertMultipleAsync<TResult, TRow>(
            Label label,
            string connectionStringName,
            CancellationToken cancellationToken,
            string sql,
            IEnumerable<TRow> rows)
            where TRow : class;

        Task<TResult> WithConnectionAsync<TResult>(
            Label label,
            string connectionStringName,
            Func<IDbConnection, CancellationToken, Task<TResult>> withConnection,
            CancellationToken cancellationToken);
    }
}
