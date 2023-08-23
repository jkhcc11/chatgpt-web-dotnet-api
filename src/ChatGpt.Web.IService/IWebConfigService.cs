using System.Threading.Tasks;
using ChatGpt.Web.Dto.Dtos;

namespace ChatGpt.Web.IService
{
    /// <summary>
    /// 站点配置 服务接口
    /// </summary>
    public interface IWebConfigService
    {
        /// <summary>
        /// 根据host获取配置信息
        /// </summary>
        /// <returns></returns>
        Task<GetResourceByHostDto> GetResourceByHostAsync(string? host);
    }
}
