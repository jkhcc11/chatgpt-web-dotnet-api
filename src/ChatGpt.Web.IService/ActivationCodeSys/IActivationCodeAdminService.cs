using System.Threading.Tasks;
using ChatGpt.Web.BaseInterface;
using ChatGpt.Web.Dto.Dtos.ActivationCodeAdmin;
using ChatGpt.Web.Dto.Inputs.ActivationCodeAdmin;

namespace ChatGpt.Web.IService.ActivationCodeSys
{
    /// <summary>
    /// 卡密管理 服务接口
    /// </summary>
    public interface IActivationCodeAdminService
    {
        /// <summary>
        /// 分页获取卡密类型
        /// </summary>
        /// <returns></returns>
        Task<KdyResult<QueryPageDto<QueryPageCodeTypeDto>>> QueryPageCodeTypeAsync(QueryPageCodeTypeInput input);

        /// <summary>
        /// 创建/修改 卡密类型
        /// </summary>
        /// <returns></returns>
        Task<KdyResult> CreateAndUpdateCodeTypeAsync(CreateAndUpdateCodeTypeInput input);

        /// <summary>
        /// 删除卡密
        /// </summary>
        /// <returns></returns>
        Task<KdyResult> DeleteCodeTypeAsync(long id);

        /// <summary>
        /// 分页获取卡密
        /// </summary>
        /// <returns></returns>
        Task<KdyResult<QueryPageDto<QueryPageActivationCodeDto>>> QueryPageActivationCodeAsync(QueryPageActivationCodeInput input);

        /// <summary>
        /// 批量创建卡密
        /// </summary>
        /// <returns></returns>
        Task<KdyResult> BatchCreateActivationCodeAsync(BatchCreateActivationCodeInput input);

        /// <summary>
        /// 删除卡密
        /// </summary>
        /// <returns></returns>
        Task<KdyResult> DeleteActivationCodeAsync(long id);
    }
}
