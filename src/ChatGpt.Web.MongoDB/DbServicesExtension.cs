using ChatGpt.Web.IRepository;
using ChatGpt.Web.IRepository.ActivationCodeSys;
using ChatGpt.Web.IRepository.MessageHistory;
using ChatGpt.Web.MongoDB.Repository;
using Microsoft.Extensions.DependencyInjection;

namespace ChatGpt.Web.MongoDB
{
    public static class DbServicesExtension
    {
        /// <summary>
        /// 添加Mongodb仓储 DI
        /// </summary>
        /// <returns></returns>
        public static IServiceCollection AddMongodbRepository(this IServiceCollection services)
        {
            services.AddTransient<IGptWebMessageRepository, GptWebMessageRepository>();
            services.AddTransient<IActivationCodeRepository, ActivationCodeRepository>();
            services.AddTransient<IPerUseActivationCodeRecordRepository, PerUseActivationCodeRecordRepository>();
            services.AddTransient<IActivationCodeTypeV2Repository, ActivationCodeTypeV2Repository>();
            services.AddTransient<IGptWebConfigRepository, GptWebConfigRepository>();
            return services;
        }
    }
}
