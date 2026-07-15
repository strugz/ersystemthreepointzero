using System.Transactions;
using ERSystem.Web.Application.Common;

namespace ERSystem.Web.Infrastructure.Services;

public sealed class EfTransactionRunner : ITransactionRunner
{
    public async Task<T> ExecuteSerializableAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        using var scope = new TransactionScope(
            TransactionScopeOption.Required,
            new TransactionOptions { IsolationLevel = IsolationLevel.Serializable, Timeout = TimeSpan.FromMinutes(2) },
            TransactionScopeAsyncFlowOption.Enabled);
        var result = await action(cancellationToken);
        scope.Complete();
        return result;
    }
}
