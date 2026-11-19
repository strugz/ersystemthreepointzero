using ERFWeb.Application.Common;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Administration;

/// <summary>Checks a job grade against the <c>job_grades</c> master data.</summary>
internal static class JobGradeCheck
{
    /// <summary>
    /// The stored code of the active grade <paramref name="value"/> names, in any letter case. The record's
    /// <paramref name="currentCode"/> is accepted as it is even after its grade was deactivated, so other edits
    /// to that record still save. Anything else is a 400 on <c>jobGrade</c>.
    /// </summary>
    public static async Task<string> RequireAsync(ErfDbContext db, string? value, string? currentCode, CancellationToken ct)
    {
        var wanted = value?.Trim();
        if (string.IsNullOrEmpty(wanted))
        {
            throw new ValidationException("jobGrade", "Job grade is required.");
        }

        if (currentCode is not null && string.Equals(wanted, currentCode, StringComparison.Ordinal)
            && await db.JobGrades.AnyAsync(g => g.Code == currentCode, ct))
        {
            return currentCode;
        }

        var lower = wanted.ToLower();
        return await db.JobGrades.AsNoTracking()
                   .Where(g => g.IsActive && g.Code.ToLower() == lower)
                   .Select(g => g.Code)
                   .FirstOrDefaultAsync(ct)
               ?? throw new ValidationException("jobGrade", "Pick a job grade from the list.");
    }
}
