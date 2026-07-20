using ERSystem.Reminders.Worker;
using ERSystem.Web.Infrastructure.Configuration;

var builder = Host.CreateApplicationBuilder(args);
var protectedSettingsPath = GetProtectedSettingsPath(args);
if (protectedSettingsPath is not null)
{
    if (!Path.IsPathFullyQualified(protectedSettingsPath) || !File.Exists(protectedSettingsPath))
    {
        throw new InvalidOperationException("The --settings value must be an existing absolute protected configuration file.");
    }

    builder.Configuration.AddJsonFile(protectedSettingsPath, optional: false, reloadOnChange: true);
    builder.Configuration.AddEnvironmentVariables();
}

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "ER System Approval Reminders";
});
builder.Services.AddApprovalReminderInfrastructure(builder.Configuration);
builder.Services.AddHostedService<ApprovalReminderWorker>();

await builder.Build().RunAsync();

static string? GetProtectedSettingsPath(string[] commandLineArguments)
{
    for (var index = 0; index < commandLineArguments.Length; index++)
    {
        if (!commandLineArguments[index].Equals("--settings", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (index + 1 >= commandLineArguments.Length || string.IsNullOrWhiteSpace(commandLineArguments[index + 1]))
        {
            throw new InvalidOperationException("--settings requires an absolute configuration file path.");
        }

        return commandLineArguments[index + 1];
    }

    return null;
}
