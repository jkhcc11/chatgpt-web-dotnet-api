using System.Collections.Generic;
using System.Threading.Tasks;
using ChatGpt.Web.Entity;

namespace ChatGpt.Web.IRepository
{
    /// <summary>
    /// 站点配置 仓储接口
    /// </summary>
    public interface IGptWebConfigRepository
    {
        /// <summary>
        /// 创建
        /// </summary>
        /// <returns></returns>
        Task<bool> CreateAsync(GptWebConfig entity);

        /// <summary>
        /// 获取所有站点配置
        /// </summary>
        /// <returns></returns>
        Task<List<GptWebConfig>> GetAllConfigAsync();

        /// <summary>
        /// 修改
        /// </summary>
        /// <returns></returns>
        Task<bool> UpdateAsync(GptWebConfig entity);

    }
}
