using Greg.Xrm.Command;
using Greg.Xrm.Command.Commands.Column.Builders;
using Greg.Xrm.Command.Services.CommandHistory;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Pluralization;
using Greg.Xrm.Command.Services.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<ICommandLineArguments>(new CommandLineArguments(args));
builder.Services.RegisterCommandExecutors(typeof(CommandAttribute).Assembly);
builder.Services.AddTransient<ICommandExecutorFactory, CommandExecutorFactory>();
builder.Services.AddTransient<IPluralizationFactory, PluralizationFactory>();
builder.Services.AddTransient<ISettingsRepository, SettingsRepository>();
builder.Services.AddSingleton<IOrganizationServiceRepository, OrganizationServiceRepository>();
builder.Services.AddSingleton<IOutput, OutputToConsole>();
builder.Services.AddTransient<IAttributeMetadataBuilderFactory, AttributeMetadataBuilderFactory>();
builder.Services.AddTransient<IHistoryTracker, HistoryTracker>();

builder.Services.AddHostedService<HostedService>();

builder.Logging.ClearProviders();
builder.Logging.AddDebug();

using var host = builder.Build();
host.Run();