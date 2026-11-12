using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SangRental.Application.CatalogFleet;

namespace SangRental.Api;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, code) = exception switch
        {
            CatalogFleetRuleException { Code: "vehicle.not_found" } => (404, "vehicle.not_found"),
            CatalogFleetRuleException rule => (409, rule.Code),
            ValidationException => (400, "request.invalid"),
            UnauthorizedAccessException => (403, "auth.forbidden"),
            PreconditionRequiredException => (428, "concurrency.if_match_required"),
            DbUpdateConcurrencyException => (412, "concurrency.resource_changed"),
            DbUpdateException { InnerException: SqlException sql } when sql.Number is 2601 or 2627 => (409, "resource.duplicate"),
            DbUpdateException { InnerException: SqlException sql } when sql.Number == 547 => (409, "resource.constraint_violation"),
            SqlException { Number: 1205 } => (409, "concurrency.retry_request"),
            SqlException => (503, "database.unavailable_or_schema_pending"),
            _ => (500, "server.error")
        };
        await AdminEndpoints.Problem(status, code).ExecuteAsync(context);
        return true;
    }
}
