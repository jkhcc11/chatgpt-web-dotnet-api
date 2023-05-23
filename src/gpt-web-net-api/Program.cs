using ChatGpt.Web.BaseInterface;
using ChatGpt.Web.BaseInterface.Options;
using ChatGpt.Web.IRepository.ActivationCodeSys;
using ChatGpt.Web.IRepository.MessageHistory;
using ChatGpt.Web.IService.OpenAiApi;
using ChatGpt.Web.LiteDatabase;
using ChatGpt.Web.LiteDatabase.Repository;
using ChatGpt.Web.NetCore.Services;
using GptWeb.DotNet.Api.JsonConvert;
using GptWeb.DotNet.Api.ServicesExtensiones;
using LiteDB;
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
services.AddRepository();
services.AddServices();

//»°≈‰÷√◊¢»Î
var config = builder.Services.BuildServiceProvider().GetService<IConfiguration>();
services.AddSingleton(_ => new IdGenerateExtension(new IdWorker(1, 1)));
services.AddTransient(_ =>
{
    var connectionString = config.GetValue<string>("ConnectionStrings:LiteDb");
    return new LiteDatabase(connectionString);
});
services.AddTransient(_ =>
{
    var connectionString = config.GetValue<string>("ConnectionStrings:LiteDbLog");
    return new LogLiteDatabase(connectionString);
});
services.Configure<ChatGptWebConfig>(config.GetSection("ChatGptWebConfig"));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(corsBuilder =>
    {
        corsBuilder.WithOrigins(config.GetValue<string>("CorsHosts").Split(","))
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseAuthorization();
app.UseCors();

app.MapControllers();
app.Run();
