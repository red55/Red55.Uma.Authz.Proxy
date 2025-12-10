
using System.Reflection;

using Red55.Uma.Authz.Proxy.Models;

using Serilog;

using Yunqi.Common.Config;
using Yunqi.Common.Web;

Log.Logger = Yunqi.Common.Logging.Default ();
Yunqi.Common.Logging.ShowBanner (Assembly.GetExecutingAssembly ());


var builder = WebApplication.CreateBuilder (args);
_ = builder.AddConfigDefaults (args);
_ = builder.AddSerilogWithDefaults (true);

_ = InitConfig.LoadConfigFromSection<AppConfig, ValidateAppConfig> (builder, nameof (AppConfig));

builder.Services.AddReverseProxy ()
    .AddTransformFactory<Red55.Uma.Authz.Proxy.Yarp.TransformFactory> ()
    .LoadFromConfig (builder.Configuration.GetSection ("ReverseProxy"));

var app = builder.Build ();

app.MapReverseProxy ();

await app.RunAsync ();

