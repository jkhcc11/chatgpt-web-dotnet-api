using System.Collections.Generic;
using System.Threading.Tasks;
using ChatGpt.Web.Entity.Enums;
using ChatGpt.Web.Entity.ActivationCodeSys;

namespace ChatGpt.Web.IRepository.ActivationCodeSys
{
    /// <summary>
    /// 卡密信息 仓储接口
    /// </summary>
    public interface IActivationCodeRepository
    {
        /// <summary>
        /// 卡密号查卡密
        /// </summary>
        /// <returns></returns>
        Task<ActivationCode?> GetActivationCodeByCardNoAsync(string cardNo);

        /// <summary>
        /// 创建卡密
        /// </summary>
        /// <returns></returns>
        Task<bool> CreateAsync(List<ActivationCode> entities);

        /// <summary>
        /// 更新
        /// </summary>
        /// <returns></returns>
        Task<bool> UpdateAsync(ActivationCode entity);

        /// <summary>
        ///  根据类型获取卡密
        /// </summary>
        /// <param name="codeType">卡密类型</param>
        /// <returns></returns>
        Task<List<ActivationCode>> QueryActivationCodeByTypeAsync(ActivationCodeType? codeType);
    }
}
