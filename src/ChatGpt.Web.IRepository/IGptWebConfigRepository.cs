using System.Collections.Generic;
using System.Threading.Tasks;
using ChatGpt.Web.BaseInterface;
using ChatGpt.Web.Entity;

namespace ChatGpt.Web.IRepository
{
    /// <summary>
    /// 站点配置 仓储接口
    /// </summary>
    public interface IGptWebConfigRepository : IBaseRepository<GptWebConfig, long>
    {
        /// <summary>
        /// 获取所有站点配置
        /// </summary>
        /// <returns></returns>
        Task<List<GptWebConfig>> GetAllConfigAsync();
    }
}
