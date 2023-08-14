using ChatGpt.Web.Dto;
using ChatGpt.Web.IService.ActivationCodeSys;
using ChatGpt.Web.IService.OpenAiApi;
using ChatGpt.Web.LiteDatabase;
using ChatGpt.Web.MongoDB;
using ChatGpt.Web.NetCore.ActivationCodeSys;
using ChatGpt.Web.NetCore.Services;
using LiteDB;
using MongoDB.Driver;

namespace GptWeb.DotNet.Api.ServicesExtensiones
{
    public static class ServicesExtension
    {
        /// <summary>
        /// 添加Services DI
        /// </summary>
        /// <returns></returns>
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddAutoMapper(typeof(MapperProfile));
            services.AddTransient<IOpenAiDashboardApiHttpApi, OpenAiDashboardApiHttpApi>();
            services.AddTransient<IOpenAiHttpApi, OpenAiHttpApi>();
            services.AddTransient<IActivationCodeService, ActivationCodeService>();
            return services;
        }

        /// <summary>
        /// 添加LiteDB
        /// </summary>
        /// <returns></returns>
        public static IServiceCollection AddLiteDb(this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetValue<string>("ConnectionStrings:LiteDb");
            var logConnectionString = configuration.GetValue<string>("ConnectionStrings:LiteDbLog");
            if (string.IsNullOrEmpty(connectionString) ||
                string.IsNullOrEmpty(logConnectionString))
            {
                throw new ArgumentNullException("未配置LiteDb连接信息，请检查ConnectionStrings:LiteDb、ConnectionStrings:LiteDbLog");
            }

            services.AddTransient(_ => new LiteDatabase(connectionString));
            services.AddTransient(_ => new LogLiteDatabase(logConnectionString));
            services.AddLiteDbRepository();
            return services;
        }

        /// <summary>
        /// 添加Mongodb
        /// </summary>
        /// <returns></returns>
        public static IServiceCollection AddMongodb(this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetValue<string>("ConnectionStrings:Mongodb");
            var databaseName = configuration.GetValue<string>("ConnectionStrings:MongodbDatabaseName");
            if (string.IsNullOrEmpty(connectionString) ||
                string.IsNullOrEmpty(databaseName))
            {
                throw new ArgumentNullException("未配置Mongodb连接信息，请检查ConnectionStrings:Mongodb、ConnectionStrings:MongodbDatabaseName");
            }

            services.AddSingleton(_ =>
                new GptWebMongodbContext(new MongoClient(connectionString), databaseName));
            services.AddMongodbRepository();
            return services;
        }
    }
}
