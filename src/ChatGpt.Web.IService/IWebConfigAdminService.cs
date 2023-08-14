using ChatGpt.Web.BaseInterface;
using System.Threading.Tasks;
using ChatGpt.Web.Dto.Dtos;
using ChatGpt.Web.Dto.Inputs;

namespace ChatGpt.Web.IService
{
    /// <summary>
    /// 配置管理 服务接口
    /// </summary>
    public interface IWebConfigAdminService
    {
        /// <summary>
        /// 分页获取站点配置
        /// </summary>
        /// <returns></returns>
        Task<KdyResult<QueryPageDto<QueryPageWebConfigDto>>> QueryPageWebConfigAsync(QueryPageWebConfigInput input);

        /// <summary>
        /// 创建/修改 站点配置
        /// </summary>
        /// <returns></returns>
        Task<KdyResult> CreateAndUpdateWebConfigAsync(CreateAndUpdateWebConfigInput input);

        /// <summary>
        /// 删除站点配置
        /// </summary>
        /// <returns></returns>
        Task<KdyResult> DeleteWebConfigAsync(long id);
    }
}
