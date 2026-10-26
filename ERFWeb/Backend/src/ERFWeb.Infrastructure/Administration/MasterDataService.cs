using System.Text.Json;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Administration;
using ERFWeb.Domain;
using ERFWeb.Domain.Reporting;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Administration;

/// <summary>
/// Master data maintained by the System Administrator (departments, cost centers, currencies,
/// sites). Rows are never deleted, only deactivated, because reports and users reference them.
/// A cost center also carries one overall budget per calendar year (monitoring only).
/// Every change writes an <c>Admin.MasterDataChanged</c> audit row in the same transaction.
/// </summary>
public sealed class MasterDataService(
    ErfDbContext db,
    ICurrentUser user,
    IClock clock,
    ITransactionRunner tx,
    IWorkflowAuditWriter audit) : IMasterDataService
{
    private const int MaxCode = 32;
    private const int MaxName = 200;

    // ---------- Departments ----------

    public Task<IReadOnlyList<DepartmentDto>> ListDepartmentsAsync(bool includeInactive, CancellationToken ct = default) =>
        Query(
            from d in db.Departments.AsNoTracking()
            where includeInactive || d.IsActive
            join sj in db.DepartmentSignatories.AsNoTracking() on d.Id equals sj.DepartmentId into ss
            from s in ss.DefaultIfEmpty()
            orderby d.Name
            select new DepartmentDto(d.Id, d.Code, d.Name, d.IsActive, EF.Property<uint>(d, DbContextExtensions.RowVersionProperty),
                s != null ? s.ReviewerInitials : null), ct);

    public async Task<DepartmentDto> CreateDepartmentAsync(SaveDepartmentRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateCodeName(request.Code, request.Name, codeRequired: false);
        ValidateReviewedBy(request.ReviewedBy);
        var code = Clean(request.Code);
        var reviewedBy = Clean(request.ReviewedBy);

        var id = await tx.ExecuteAsync(async token =>
        {
            if (code is not null && await db.Departments.AnyAsync(d => d.Code == code, token))
            {
                throw new ValidationException("code", "A department with that code already exists.");
            }

            var entity = new Department { Code = code, Name = request.Name.Trim(), IsActive = request.IsActive };
            db.Departments.Add(entity);
            await db.SaveChangesAsync(token);
            if (reviewedBy is not null)
            {
                db.DepartmentSignatories.Add(new DepartmentSignatory { DepartmentId = entity.Id, ReviewerInitials = reviewedBy });
                await db.SaveChangesAsync(token);
            }

            await AuditAsync("department", "created", entity.Id, new { entity.Code, entity.Name, entity.IsActive, ReviewedBy = reviewedBy }, token);
            return entity.Id;
        }, ct);

        return (await ListDepartmentsAsync(true, ct)).First(d => d.Id == id);
    }

    public async Task<DepartmentDto> UpdateDepartmentAsync(long id, SaveDepartmentRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateCodeName(request.Code, request.Name, codeRequired: false);
        ValidateReviewedBy(request.ReviewedBy);
        var code = Clean(request.Code);

        await tx.ExecuteAsync(async token =>
        {
            var entity = await db.Departments.FirstOrDefaultAsync(d => d.Id == id, token)
                ?? throw new NotFoundException("Department not found.");
            if (code is not null && await db.Departments.AnyAsync(d => d.Code == code && d.Id != id, token))
            {
                throw new ValidationException("code", "A department with that code already exists.");
            }

            var signatory = await db.DepartmentSignatories.FirstOrDefaultAsync(s => s.DepartmentId == id, token);
            var before = new { entity.Code, entity.Name, entity.IsActive, ReviewedBy = signatory?.ReviewerInitials };
            entity.Code = code;
            entity.Name = request.Name.Trim();
            entity.IsActive = request.IsActive;

            // A client that predates the field sends none; leave the stored reviewer as it is.
            if (request.ReviewedBy is not null)
            {
                var reviewedBy = Clean(request.ReviewedBy);
                if (signatory is not null)
                {
                    signatory.ReviewerInitials = reviewedBy;
                }
                else if (reviewedBy is not null)
                {
                    signatory = new DepartmentSignatory { DepartmentId = id, ReviewerInitials = reviewedBy };
                    db.DepartmentSignatories.Add(signatory);
                }
            }

            db.SetRowVersion(entity, request.RowVersion ?? throw new ValidationException("rowVersion", "rowVersion is required for an update."));
            await db.SaveWithConcurrencyAsync(token);
            await AuditAsync("department", "updated", id,
                new { before, after = new { entity.Code, entity.Name, entity.IsActive, ReviewedBy = signatory?.ReviewerInitials } }, token);
        }, ct);

        return (await ListDepartmentsAsync(true, ct)).First(d => d.Id == id);
    }

    // ---------- Cost centers ----------

    public async Task<IReadOnlyList<CostCenterDto>> ListCostCentersAsync(bool includeInactive, CancellationToken ct = default)
    {
        var rows = await (
            from c in db.CostCenters.AsNoTracking()
            where includeInactive || c.IsActive
            join dj in db.Departments.AsNoTracking() on c.DepartmentId equals (long?)dj.Id into ds
            from d in ds.DefaultIfEmpty()
            orderby c.Code
            select new
            {
                c.Id,
                c.Code,
                c.Name,
                c.DepartmentId,
                DepartmentName = d != null ? d.Name : null,
                c.IsActive,
                RowVersion = EF.Property<uint>(c, DbContextExtensions.RowVersionProperty)
            }).ToListAsync(ct);
        var ids = rows.Select(r => r.Id).ToList();
        var budgets = (await db.CostCenterBudgets.AsNoTracking().Where(b => ids.Contains(b.CostCenterId)).ToListAsync(ct))
            .ToLookup(b => b.CostCenterId);
        return rows.Select(r => new CostCenterDto(r.Id, r.Code, r.Name, r.DepartmentId, r.DepartmentName, r.IsActive, r.RowVersion,
            budgets[r.Id].OrderByDescending(b => b.BudgetYear).Select(b => new CostCenterBudgetDto(b.BudgetYear, b.Amount)).ToList())).ToList();
    }

    public async Task<CostCenterDto> CreateCostCenterAsync(SaveCostCenterRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateCodeName(request.Code, request.Name, codeRequired: true);
        ValidateBudgets(request.Budgets);
        var code = request.Code.Trim();

        var id = await tx.ExecuteAsync(async token =>
        {
            if (await db.CostCenters.AnyAsync(c => c.Code == code, token))
            {
                throw new ValidationException("code", "A cost center with that code already exists.");
            }

            await EnsureCostCenterDepartmentAsync(request.DepartmentId, request.IsActive, null, token);
            var entity = new CostCenter { Code = code, Name = request.Name.Trim(), DepartmentId = request.DepartmentId, IsActive = request.IsActive };
            db.CostCenters.Add(entity);
            await db.SaveChangesAsync(token);
            var budgets = await SaveBudgetsAsync(entity.Id, request.Budgets, token);
            await AuditAsync("costCenter", "created", entity.Id, new { entity.Code, entity.Name, entity.DepartmentId, entity.IsActive, budgets }, token);
            return entity.Id;
        }, ct);

        return (await ListCostCentersAsync(true, ct)).First(c => c.Id == id);
    }

    public async Task<CostCenterDto> UpdateCostCenterAsync(long id, SaveCostCenterRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateCodeName(request.Code, request.Name, codeRequired: true);
        ValidateBudgets(request.Budgets);
        var code = request.Code.Trim();

        await tx.ExecuteAsync(async token =>
        {
            var entity = await db.CostCenters.FirstOrDefaultAsync(c => c.Id == id, token)
                ?? throw new NotFoundException("Cost center not found.");
            if (await db.CostCenters.AnyAsync(c => c.Code == code && c.Id != id, token))
            {
                throw new ValidationException("code", "A cost center with that code already exists.");
            }

            await EnsureCostCenterDepartmentAsync(request.DepartmentId, request.IsActive, id, token);
            var before = new { entity.Code, entity.Name, entity.DepartmentId, entity.IsActive };
            entity.Code = code;
            entity.Name = request.Name.Trim();
            entity.DepartmentId = request.DepartmentId;
            entity.IsActive = request.IsActive;
            db.SetRowVersion(entity, request.RowVersion ?? throw new ValidationException("rowVersion", "rowVersion is required for an update."));
            await db.SaveWithConcurrencyAsync(token);
            var budgets = await SaveBudgetsAsync(id, request.Budgets, token);
            await AuditAsync("costCenter", "updated", id,
                new { before, after = new { entity.Code, entity.Name, entity.DepartmentId, entity.IsActive }, budgets }, token);
        }, ct);

        return (await ListCostCentersAsync(true, ct)).First(c => c.Id == id);
    }

    // ---------- Currencies ----------

    public Task<IReadOnlyList<CurrencyDto>> ListCurrenciesAsync(bool includeInactive, CancellationToken ct = default) =>
        Query(db.Currencies.AsNoTracking().Where(c => includeInactive || c.IsActive).OrderByDescending(c => c.IsDefault).ThenBy(c => c.Code)
            .Select(c => new CurrencyDto(c.Code, c.Name, c.IsDefault, c.IsActive, EF.Property<uint>(c, DbContextExtensions.RowVersionProperty))), ct);

    public async Task<CurrencyDto> CreateCurrencyAsync(SaveCurrencyRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var code = ValidateCurrencyCode(request.Code);
        ValidateCodeName(code, request.Name, codeRequired: true);

        await tx.ExecuteAsync(async token =>
        {
            if (await db.Currencies.AnyAsync(c => c.Code == code, token))
            {
                throw new ValidationException("code", "That currency already exists.");
            }

            if (request.IsDefault)
            {
                await ClearDefaultCurrencyAsync(token);
            }

            db.Currencies.Add(new Currency { Code = code, Name = request.Name.Trim(), IsDefault = request.IsDefault, IsActive = request.IsActive });
            await db.SaveChangesAsync(token);
            await AuditAsync("currency", "created", code, new { code, request.Name, request.IsDefault, request.IsActive }, token);
        }, ct);

        return (await ListCurrenciesAsync(true, ct)).First(c => c.Code == code);
    }

    public async Task<CurrencyDto> UpdateCurrencyAsync(string code, SaveCurrencyRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var key = ValidateCurrencyCode(code);
        ValidateCodeName(key, request.Name, codeRequired: true);

        await tx.ExecuteAsync(async token =>
        {
            var entity = await db.Currencies.FirstOrDefaultAsync(c => c.Code == key, token)
                ?? throw new NotFoundException("Currency not found.");
            if (request.IsDefault && !entity.IsDefault)
            {
                await ClearDefaultCurrencyAsync(token);
            }

            if (!request.IsActive && entity.IsDefault)
            {
                throw new ValidationException("isActive", "The default currency cannot be deactivated. Choose another default first.");
            }

            var before = new { entity.Name, entity.IsDefault, entity.IsActive };
            entity.Name = request.Name.Trim();
            entity.IsDefault = request.IsDefault;
            entity.IsActive = request.IsActive;
            db.SetRowVersion(entity, request.RowVersion ?? throw new ValidationException("rowVersion", "rowVersion is required for an update."));
            await db.SaveWithConcurrencyAsync(token);
            await AuditAsync("currency", "updated", key, new { before, after = new { entity.Name, entity.IsDefault, entity.IsActive } }, token);
        }, ct);

        return (await ListCurrenciesAsync(true, ct)).First(c => c.Code == key);
    }

    // ---------- Sites ----------

    public Task<IReadOnlyList<SiteDto>> ListSitesAsync(bool includeInactive, CancellationToken ct = default) =>
        Query(db.Sites.AsNoTracking().Where(s => includeInactive || s.IsActive).OrderBy(s => s.Code)
            .Select(s => new SiteDto(s.Id, s.Code, s.Name, s.IsActive, EF.Property<uint>(s, DbContextExtensions.RowVersionProperty))), ct);

    public async Task<SiteDto> CreateSiteAsync(SaveSiteRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateCodeName(request.Code, request.Name, codeRequired: true);
        var code = request.Code.Trim();

        var id = await tx.ExecuteAsync(async token =>
        {
            if (await db.Sites.AnyAsync(s => s.Code == code, token))
            {
                throw new ValidationException("code", "A site with that code already exists.");
            }

            var entity = new Site { Code = code, Name = request.Name.Trim(), IsActive = request.IsActive };
            db.Sites.Add(entity);
            await db.SaveChangesAsync(token);
            await AuditAsync("site", "created", entity.Id, new { entity.Code, entity.Name, entity.IsActive }, token);
            return entity.Id;
        }, ct);

        return (await ListSitesAsync(true, ct)).First(s => s.Id == id);
    }

    public async Task<SiteDto> UpdateSiteAsync(long id, SaveSiteRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateCodeName(request.Code, request.Name, codeRequired: true);
        var code = request.Code.Trim();

        await tx.ExecuteAsync(async token =>
        {
            var entity = await db.Sites.FirstOrDefaultAsync(s => s.Id == id, token)
                ?? throw new NotFoundException("Site not found.");
            if (await db.Sites.AnyAsync(s => s.Code == code && s.Id != id, token))
            {
                throw new ValidationException("code", "A site with that code already exists.");
            }

            var before = new { entity.Code, entity.Name, entity.IsActive };
            entity.Code = code;
            entity.Name = request.Name.Trim();
            entity.IsActive = request.IsActive;
            db.SetRowVersion(entity, request.RowVersion ?? throw new ValidationException("rowVersion", "rowVersion is required for an update."));
            await db.SaveWithConcurrencyAsync(token);
            await AuditAsync("site", "updated", id, new { before, after = new { entity.Code, entity.Name, entity.IsActive } }, token);
        }, ct);

        return (await ListSitesAsync(true, ct)).First(s => s.Id == id);
    }

    // ---------- Expense categories (fixed by the R47 form) ----------

    public IReadOnlyList<ExpenseCategoryDto> ListExpenseCategories() =>
    [
        new(ExpenseCategory.Meals, "Meals", "MEALS"),
        new(ExpenseCategory.Transportation, "Transportation", "TRANSP."),
        new(ExpenseCategory.Toll, "Toll", "TOLL"),
        new(ExpenseCategory.Parking, "Parking", "PARKING"),
        new(ExpenseCategory.Others, "Others", "OTHERS"),
        new(ExpenseCategory.Representation, "Representation", "Representation"),
    ];

    // ---------- Helpers ----------

    private static async Task<IReadOnlyList<T>> Query<T>(IQueryable<T> query, CancellationToken ct) =>
        await query.ToListAsync(ct);

    private Task AuditAsync(string entity, string action, object key, object detail, CancellationToken ct) =>
        audit.WriteAsync(new WorkflowAuditEntry(
            AdminAuditEvents.MasterDataChanged, null, user.UserId,
            JsonSerializer.Serialize(new { entity, action, key, detail })), ct);

    /// <summary>A cost center belongs to one department, and a department has at most one active cost center.</summary>
    private async Task EnsureCostCenterDepartmentAsync(long? departmentId, bool isActive, long? costCenterId, CancellationToken ct)
    {
        if (departmentId is not { } d)
        {
            throw new ValidationException("departmentId", "Choose the department this cost center belongs to.");
        }

        if (!await db.Departments.AnyAsync(x => x.Id == d, ct))
        {
            throw new ValidationException("departmentId", "The department does not exist.");
        }

        if (isActive && await db.CostCenters.AnyAsync(c => c.DepartmentId == d && c.IsActive && c.Id != costCenterId, ct))
        {
            throw new ValidationException("departmentId", "That department already has an active cost center.");
        }
    }

    private static void ValidateBudgets(IReadOnlyList<CostCenterBudgetInput>? budgets)
    {
        if (budgets is null)
        {
            return;
        }

        var errors = new Dictionary<string, string[]>();
        foreach (var b in budgets)
        {
            var error = CostCenterBudgetRules.YearError(b.Year) ?? (b.Amount is { } amount ? CostCenterBudgetRules.AmountError(amount) : null);
            if (error is not null)
            {
                errors[$"budget{b.Year}"] = [error];
            }
        }

        if (budgets.GroupBy(b => b.Year).Any(g => g.Count() > 1))
        {
            errors["budgets"] = ["Each year may appear once."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    /// <summary>Sets or clears the given years' budgets; returns what changed, for the audit row.</summary>
    private async Task<object?> SaveBudgetsAsync(long costCenterId, IReadOnlyList<CostCenterBudgetInput>? budgets, CancellationToken ct)
    {
        if (budgets is null || budgets.Count == 0)
        {
            return null;
        }

        var years = budgets.Select(b => b.Year).ToList();
        var existing = await db.CostCenterBudgets.Where(b => b.CostCenterId == costCenterId && years.Contains(b.BudgetYear)).ToListAsync(ct);
        var changes = new List<object>();
        foreach (var input in budgets)
        {
            var row = existing.FirstOrDefault(b => b.BudgetYear == input.Year);
            if (row?.Amount == input.Amount)
            {
                continue;
            }

            changes.Add(new { year = input.Year, before = row?.Amount, after = input.Amount });
            if (input.Amount is not { } amount)
            {
                db.CostCenterBudgets.Remove(row!);
                continue;
            }

            if (row is null)
            {
                row = new CostCenterBudget { CostCenterId = costCenterId, BudgetYear = input.Year };
                db.CostCenterBudgets.Add(row);
            }

            row.Amount = amount;
            row.UpdatedByUserId = user.UserId;
            row.UpdatedAt = clock.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        return changes.Count > 0 ? changes : null;
    }

    private async Task ClearDefaultCurrencyAsync(CancellationToken ct)
    {
        foreach (var current in await db.Currencies.Where(c => c.IsDefault).ToListAsync(ct))
        {
            current.IsDefault = false;
        }
    }

    private static string ValidateCurrencyCode(string? code)
    {
        var value = code?.Trim().ToUpperInvariant() ?? string.Empty;
        if (value.Length != 3 || !value.All(char.IsAsciiLetterUpper))
        {
            throw new ValidationException("code", "Enter a three-letter ISO 4217 currency code, e.g. PHP.");
        }

        return value;
    }

    private static void ValidateCodeName(string? code, string? name, bool codeRequired)
    {
        var errors = new Dictionary<string, string[]>();
        if (codeRequired && string.IsNullOrWhiteSpace(code))
        {
            errors["code"] = ["Code is required."];
        }
        else if (code is not null && code.Trim().Length > MaxCode)
        {
            errors["code"] = [$"Code must be at most {MaxCode} characters."];
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            errors["name"] = ["Name is required."];
        }
        else if (name.Trim().Length > MaxName)
        {
            errors["name"] = [$"Name must be at most {MaxName} characters."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private static void ValidateReviewedBy(string? reviewedBy)
    {
        if (reviewedBy is not null && reviewedBy.Trim().Length > SystemSettingKeys.R47ReviewedByMaxLength)
        {
            throw new ValidationException("reviewedBy", $"Reviewed by must be at most {SystemSettingKeys.R47ReviewedByMaxLength} characters.");
        }
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
