using DiscordAlwaysOn;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Services
    .AddHostedService<AlwaysOnService>()
    .AddOptions<AlwaysOnOptions>()
    .BindConfiguration("AlwaysOn");

var host = builder.Build();
host.Run();