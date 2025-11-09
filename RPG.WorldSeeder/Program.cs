using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RPG.Core.Interfaces;
using RPG.Core.Services.World;
using RPG.Infrastructure;
using RPG.WorldSeeder.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
	.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
	.AddJsonFile("../RPG.Infrastructure/appsettings.infrastructure.json", optional: true, reloadOnChange: true)
	.AddJsonFile("../RPG.Infrastructure/appsettings.infrastructure.Development.json", optional: true, reloadOnChange: true)
	.AddEnvironmentVariables();

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.ApplicationName);
builder.Services.AddSingleton<IWorldStateService, WorldStateService>();
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
