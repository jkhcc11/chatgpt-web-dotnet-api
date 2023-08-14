using System;
using System.Collections.Generic;
using ChatGpt.Web.IService;
using System.Threading.Tasks;
using ChatGpt.Web.Dto.Dtos;
using System.Linq;
using ChatGpt.Web.IRepository;
using Microsoft.Extensions.Caching.Memory;
using ChatGpt.Web.Entity;

namespace ChatGpt.Web.NetCore.Services
{
    /// <summary>
    /// 站点配置 服务实现
    /// </summary>
    public class WebConfigService : IWebConfigService
    {
        private readonly IGptWebConfigRepository _gptWebConfigRepository;
        private readonly IMemoryCache _memoryCache;

        public WebConfigService(IGptWebConfigRepository gptWebConfigRepository,
            IMemoryCache memoryCache)
        {
            _gptWebConfigRepository = gptWebConfigRepository;
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// 根据host获取配置信息
        /// </summary>
        /// <returns></returns>
        public async Task<GetResourceByHostDto> GetResourceByHostAsync(string? host)
        {
            var cacheKey = $"{nameof(GptWebConfig)}_{host}";
            var cacheV = await _memoryCache.GetOrCreateAsync(cacheKey, async (cacheEntry) =>
            {
                var allConfig = await _gptWebConfigRepository.GetAllConfigAsync();
                //优先host 没有默认
                var currentConfig = allConfig.FirstOrDefault(a => a.SubDomainHost == host) ??
                                    allConfig.First(a => string.IsNullOrEmpty(a.SubDomainHost));

                var defaultModel = new List<string>()
                {
                    "gpt-3.5-turbo", "gpt-3.5-turbo-16k"
                };
                if (currentConfig.SupportModel != null &&
                    currentConfig.SupportModel.Any())
                {
                    defaultModel = currentConfig.SupportModel;
                }

                var result = new GetResourceByHostDto()
                {
                    Avatar = currentConfig.Avatar ?? "",
                    Name = currentConfig.Name ?? "",
                    Description = currentConfig.Description ?? "",
                    HomeBtnHtml = currentConfig.HomeBtnHtml ?? "",

                    FreeCode = currentConfig.FreeCode3 ?? "ai.gpt-666.com",
                    FreeCode4 = currentConfig.FreeCode4 ?? "",
                    EveryDayFreeTimes = currentConfig.FreeTimesWith3,
                    EveryDayFreeTimes4 = currentConfig.FreeTimesWith4,

                    Wximg = currentConfig.Wximg ?? "",
                    Wxremark = currentConfig.Wxremark ?? "加v防走丢",
                    SupportModel = defaultModel
                        .Select(a => new NSelectItem()
                        {
                            Label = a,
                            Value = a,
                        }).ToList(),
                };

                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
                return result;
            });

            return cacheV;
        }
    }
}
