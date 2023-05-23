using ChatGpt.Web.IRepository.ActivationCodeSys;
using ChatGpt.Web.IRepository.MessageHistory;
using ChatGpt.Web.IService.OpenAiApi;
using ChatGpt.Web.LiteDatabase.Repository;
using ChatGpt.Web.NetCore.Services;

namespace GptWeb.DotNet.Api.ServicesExtensiones
{
    public static class ServicesExtension
    {
        /// <summary>
        /// 添加仓储 DI
        /// </summary>
        /// <returns></returns>
        public static IServiceCollection AddRepository(this IServiceCollection services)
        {
            services.AddTransient<IGptWebMessageRepository, GptWebMessageRepository>();
            services.AddTransient<IActivationCodeRepository, ActivationCodeRepository>();
            services.AddTransient<IPerUseActivationCodeRecordRepository, PerUseActivationCodeRecordRepository>();
            return services;
        }

        /// <summary>
        /// 添加Services DI
        /// </summary>
        /// <returns></returns>
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddTransient<IOpenAiDashboardApiHttpApi, OpenAiDashboardApiHttpApi>();
            services.AddTransient<IOpenAiHttpApi, OpenAiHttpApi>();
            return services;
        }
    }
}
