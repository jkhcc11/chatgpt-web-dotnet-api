using System.Collections.Generic;
using System.Threading.Tasks;
using ChatGpt.Web.BaseInterface;
using ChatGpt.Web.Entity.ActivationCodeSys;

namespace ChatGpt.Web.IRepository.ActivationCodeSys
{
    /// <summary>
    /// 卡密信息 仓储接口
    /// </summary>
    public interface IActivationCodeRepository : IBaseRepository<ActivationCode, long>
    {
        /// <summary>
        /// 卡密号查卡密
        /// </summary>
        /// <returns></returns>
        Task<ActivationCode?> GetActivationCodeByCardNoAsync(string cardNo);

        /// <summary>
        ///  根据类型获取卡密
        /// </summary>
        /// <param name="codeTypeId">卡密类型ID</param>
        /// <returns></returns>
        Task<List<ActivationCode>> QueryActivationCodeByTypeAsync(long? codeTypeId);
    }
}
