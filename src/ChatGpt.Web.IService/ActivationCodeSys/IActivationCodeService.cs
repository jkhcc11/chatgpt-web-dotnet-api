using ChatGpt.Web.BaseInterface;
using ChatGpt.Web.Entity.ActivationCodeSys;
using System.Threading.Tasks;

namespace ChatGpt.Web.IService.ActivationCodeSys
{
    /// <summary>
    /// 卡密 服务接口
    /// </summary>
    public interface IActivationCodeService
    {
        /// <summary>
        /// 根据卡密获取卡密缓存
        /// </summary>
        /// <returns></returns>
        Task<ActivationCode?> GetCardInfoByCacheAsync(string cardNo);

        /// <summary>
        /// 获取卡类型缓存
        /// </summary>
        /// <returns></returns>
        Task<ActivationCodeTypeV2> GetCodeTypeByCacheAsync(long codeTypeId);

        /// <summary>
        /// 检查卡密是否有效
        /// </summary>
        /// <returns></returns>
        Task<KdyResult> CheckCardNoIsValidAsync(string cardNo);

        /// <summary>
        /// 首次检查卡密是否有效
        /// </summary>
        /// <remarks>
        ///  首次验卡校验时,需要设置激活时间
        /// </remarks>
        /// <returns></returns>
        Task<KdyResult> CheckCardNoIsValidWithFirstAsync(string cardNo);

        /// <summary>
        /// 检查卡密是否有访问权限
        /// </summary>
        /// <param name="cardInfo">卡信息</param>
        /// <param name="codeType">卡密类型</param>
        /// <param name="modelId">模型Id</param>
        /// <returns></returns>
        Task<KdyResult> CheckCardNoIsAccessAsync(ActivationCode cardInfo, ActivationCodeTypeV2 codeType, string modelId);

        /// <summary>
        /// 检查卡密当天请求次数
        /// </summary>
        /// <param name="cardInfo">卡信息</param>
        /// <param name="codeType">卡密类型</param>
        /// <param name="supportModeItem">模型Item</param>
        /// <returns></returns>
        Task<KdyResult> CheckTodayCardNoTimesAsync(ActivationCode cardInfo, ActivationCodeTypeV2 codeType,
            SupportModeItem supportModeItem);

        /// <summary>
        /// 检查卡密所有访问次数
        /// </summary>
        /// <param name="cardInfo">卡信息</param>
        /// <param name="codeType">卡密类型</param>
        /// <param name="supportModeItem">模型Item</param>
        /// <returns></returns>
        Task<KdyResult> CheckCardNoTimesAsync(ActivationCode cardInfo, ActivationCodeTypeV2 codeType,
            SupportModeItem supportModeItem);
    }
}
