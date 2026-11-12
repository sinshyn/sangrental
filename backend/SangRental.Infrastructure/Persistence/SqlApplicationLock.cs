using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace SangRental.Infrastructure.Persistence;

internal static class SqlApplicationLock
{
    public static async Task AcquireAsync(SangRentalDbContext db, string resource, CancellationToken cancellationToken)
    {
        using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction()
            ?? throw new InvalidOperationException("A transaction is required for application locking.");
        command.CommandText = "DECLARE @result int; EXEC @result = sp_getapplock " +
            "@Resource = @resource, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 5000; SELECT @result;";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@resource";
        parameter.Value = resource;
        command.Parameters.Add(parameter);
        if (await command.ExecuteScalarAsync(cancellationToken) is not int result || result < 0)
            throw new InvalidOperationException("The operation lock is unavailable; retry the whole request.");
    }
}
