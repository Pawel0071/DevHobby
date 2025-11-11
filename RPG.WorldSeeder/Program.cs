using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RPG.Core.Interfaces;
using RPG.Core.Services.World;
using RPG.Infrastructure;
using RPG.WorldSeeder.Services;
using RPG.WorldSeeder.Seeders;

var builder = Host.CreateApplicationBuilder(args);

var runningInContainer = string.Equals(
	Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
	"true",
	StringComparison.OrdinalIgnoreCase);

var basePath = AppContext.BaseDirectory;

builder.Configuration.AddJsonFile(Path.Combine(basePath, "appsettings.json"), optional: true, reloadOnChange: true);

if (runningInContainer)
{
	builder.Configuration
		.AddJsonFile(Path.Combine(basePath, "appsettings.infrastructure.json"), optional: true, reloadOnChange: true)
		.AddJsonFile(Path.Combine(basePath, "appsettings.infrastructure.Development.json"), optional: true, reloadOnChange: true);
}
else
{
	builder.Configuration.AddJsonFile(Path.Combine(basePath, "appsettings.Development.json"), optional: true, reloadOnChange: true);
}

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.ApplicationName);
builder.Services.AddSingleton<IWorldStateService, WorldStateService>();
builder.Services.AddSingleton<SeedDataLoader>();
builder.Services.AddSingleton<WorldSeederService>();

var host = builder.Build();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

try
{
	logger.LogInformation("Starting world seeding...");

	var seeder = host.Services.GetRequiredService<WorldSeederService>();
	await seeder.SeedAsync();

	logger.LogInformation("World seeding completed successfully.");
	Environment.ExitCode = 0;
}
catch (Exception ex)
{
	logger.LogError(ex, "World seeding failed.");
	Environment.ExitCode = 1;
}
