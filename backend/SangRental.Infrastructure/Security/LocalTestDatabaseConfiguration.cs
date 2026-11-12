using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace SangRental.Infrastructure.Security;

public static class LocalTestDatabaseConfiguration
{
    // Explicit CLI opt-in; reuse the local credential without printing or copying it into repository files.
    public static void UseLocalTestDatabase(this IConfiguration configuration)
    {
        var connection = new SqlConnectionStringBuilder(configuration.GetConnectionString("SangRental")
            ?? throw new InvalidOperationException("Configure the local SQL connection first."));
        if (connection.DataSource is not ("localhost,1436" or "tcp:127.0.0.1,1436" or "127.0.0.1,1436"))
            throw new InvalidOperationException("Test database mode allows only the approved local SQL endpoint.");
        connection.DataSource = "tcp:127.0.0.1,1436";
        connection.InitialCatalog = "SangRentalTestDb";
        configuration["ConnectionStrings:SangRental"] = connection.ConnectionString;
    }
}
