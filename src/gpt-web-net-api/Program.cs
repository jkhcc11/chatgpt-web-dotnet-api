using ChatGpt.Web.BaseInterface;
using ChatGpt.Web.BaseInterface.Options;
using GptWeb.DotNet.Api.JsonConvert;
using GptWeb.DotNet.Api.ServicesExtensiones;
using Snowflake.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var services = builder.Services;
services.AddControllers()
    .AddJsonOptions(conf =>
    {
        conf.JsonSerializerOptions.Converters.Add(new DateTimeConverter());
    });
services.AddHttpClient();
services.AddMemoryCache();
services.AddServices();

//È¡ÅäÖÃ×¢Èë
var config = builder.Configuration;
services.AddSingleton(_ => new IdGenerateExtension(new IdWorker(1, 1)));

var dbType = config.GetValue<SupportDbType>("SupportDbType");
switch (dbType)
{
    case SupportDbType.LiteDB:
        {
            services.AddLiteDb(config);
            break;
        }
    case SupportDbType.MongoDB:
        {
            services.AddMongodb(config);
            break;
        }
}

services.Configure<ChatGptWebConfig>(config.GetSection("ChatGptWebConfig"));
services.Configure<WebResourceConfig>(config.GetSection("WebResource"));

var defaultPolicy = "AiCorsPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(defaultPolicy, corsBuilder =>
    {
        corsBuilder.WithOrigins(config.GetValue<string>("CorsHosts").Split(","))
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseAuthorization();
app.UseCors(defaultPolicy);
app.MapControllers();
app.Run();
