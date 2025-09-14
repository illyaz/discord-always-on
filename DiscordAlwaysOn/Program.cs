using DiscordAlwaysOn;
using Serilog;
using Serilog.Events;
using Serilog.Templates;
using Serilog.Templates.Themes;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Services
    .AddSerilog(s => s
        .Enrich.FromLogContext()
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Warning)
        .ReadFrom.Configuration(builder.Configuration)
        .WriteTo.Console(new ExpressionTemplate(
            "[{@t:HH:mm:ss} {@l:u3}] " +
            "{SourceContext} : {@m}\n{@x}",
            theme: TemplateTheme.Literate)))
    .AddHostedService<AlwaysOnService>()
    .AddOptions<AlwaysOnOptions>()
    .BindConfiguration("AlwaysOn");

var host = builder.Build();
host.Run();