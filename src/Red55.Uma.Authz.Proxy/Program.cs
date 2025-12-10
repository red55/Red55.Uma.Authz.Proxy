
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Red55.Uma.Authz.Proxy.Middlewares;
using Red55.Uma.Authz.Proxy.Models;

using Serilog;
using Serilog.Exceptions;
using Serilog.Exceptions.Core;
using Serilog.Templates;

const string DEFAULT_LOG_TEMPLATE =
       "[{@t:yyyy-MM-ddTHH:mm:ss} {Coalesce(CorrelationId, '0000000000000:00000000')} {@l:u3}] {@m}\n{@x}";
static void ShowBanner(Assembly assembly)
{
    var assemblyName = assembly.GetName ();
    var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute> ()?.InformationalVersion ??
        assemblyName.Version?.ToString () ?? "Unknown";

    Log.Logger.Information ("Starting up {Application} ({Version})", assemblyName.Name, version);
}

static TConfig LoadConfigFromSection<TConfig, TConfigValidator>(IHostApplicationBuilder builder,
        string sectionName = nameof (TConfig)) where TConfig : class, new()
        where TConfigValidator : class, IValidateOptions<TConfig>, new()
{
    IConfiguration configuration = builder.Configuration;

    var appConfigSection = configuration.GetRequiredSection (sectionName);
    var config = appConfigSection.Get<TConfig> (o => o.ErrorOnUnknownConfiguration = true);

    if (config is null)
    {
        config = new TConfig ();
        Log.Warning ("Configuration section {SectionName} is missing or invalid, using default configuration.",
            sectionName);
    }
    var v = new TConfigValidator ();
    var validation = v.Validate (sectionName, config);
    if (validation.Failed)
    {
        throw new Exception (validation.FailureMessage);
    }
    _ = builder.Services.AddSingleton<IValidateOptions<TConfig>> (v);

    _ = builder.Services.AddOptions<TConfig> ()
        .Bind (appConfigSection)
        .ValidateOnStart ();

    return config;
}


Log.Logger = new LoggerConfiguration ()
            .WriteTo.Console (new ExpressionTemplate (DEFAULT_LOG_TEMPLATE))
            .Enrich.FromLogContext ()
            .MinimumLevel.Information ()
            .CreateBootstrapLogger ();

ShowBanner (Assembly.GetExecutingAssembly ());


var builder = WebApplication.CreateBuilder (args);
var environment = builder.Environment.EnvironmentName;

builder.Configuration.Sources.Clear ();
_ = builder.Configuration
    .AddEnvironmentVariables ("ASPNETCORE_")
    .AddCommandLine (args)
    .AddEnvironmentVariables ("DOTNET_")
    .AddYamlFile ("appsettings.yml", optional: false)
    .AddYamlFile ($"appsettings.{environment}.yml", optional: true, reloadOnChange: true)
    .AddYamlFile ($"appsettings.{environment}.Vault.yml", optional: true, reloadOnChange: true)
    .AddUserSecrets (Assembly.GetExecutingAssembly ())
    .AddEnvironmentVariables ("");

_ = builder.Services.AddSerilog ((services, configuration) =>
{
    configuration
            .ReadFrom.Configuration (builder.Configuration)
            .ReadFrom.Services (services)
            .Enrich.FromLogContext ()
            .Enrich.WithDemystifiedStackTraces ()
            .Enrich.WithCorrelationId (addValueIfHeaderAbsence: true)
            .Enrich.WithExceptionDetails (new DestructuringOptionsBuilder ()
                .WithDefaultDestructurers ());

}
);


_ = LoadConfigFromSection<AppConfig, ValidateAppConfig> (builder, nameof (AppConfig));

builder.Services.AddReverseProxy ()
    .AddTransformFactory<Red55.Uma.Authz.Proxy.Yarp.TransformFactory> ()
    .LoadFromConfig (builder.Configuration.GetSection ("ReverseProxy"));

var app = builder.Build ();
app.UseMiddleware<CorrelationIdMiddleware> ();
app.MapReverseProxy ();

app.MapGet ("/healthz", () => Results.Ok ("Healthy"));
app.MapGet ("/healthz/ready", () => Results.Ok ("Ready"));
app.MapGet ("/healthz/live", () => Results.Ok ("Live"));

await app.RunAsync ();

