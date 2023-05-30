using System.Collections.Generic;
using System.Threading.Tasks;
using ChatGpt.Web.Entity.ActivationCodeSys;

namespace ChatGpt.Web.IRepository.ActivationCodeSys
{
    /// <summary>
    /// 卡密类型 仓储接口
    /// </summary>
    public interface IActivationCodeTypeV2Repository
    {
        /// <summary>
        /// 根据Id获取卡密类型
        /// </summary>
        /// <returns></returns>
        Task<ActivationCodeTypeV2> GetEntityByIdAsync(long codeTypeId);

        /// <summary>
        /// 创建卡密类型
        /// </summary>
        /// <returns></returns>
        Task<bool> CreateAsync(List<ActivationCodeTypeV2> entities);

        /// <summary>
        ///  获取所有卡密类型
        /// </summary>
        /// <returns></returns>
        Task<List<ActivationCodeTypeV2>> GetAllActivationCodeTypeAsync();

        /// <summary>
        /// 清空卡类型
        /// </summary>
        /// <returns></returns>
        Task<bool> DeleteAllAsync();

        /// <summary>
        /// 检查名称是否存在
        /// </summary>
        /// <returns></returns>
        Task<bool> CheckNameAsync(string name);

        /// <summary>
        /// 更新
        /// </summary>
        /// <returns></returns>
        Task<bool> UpdateAsync(ActivationCodeTypeV2 entity);
    }
}
