using System;
using System.Threading.Tasks;
using ChatGpt.Web.BaseInterface;
using ChatGpt.Web.Entity.ActivationCodeSys;

namespace ChatGpt.Web.IRepository.ActivationCodeSys
{
    /// <summary>
    /// 按次卡密记录 仓储接口
    /// </summary>
    public interface IPerUseActivationCodeRecordRepository : IBaseRepository<PerUseActivationCodeRecord, long>
    {
        /// <summary>
        /// 根据日期模型分组统计次数
        /// </summary>
        /// <param name="date">日期 为空查所有</param>
        /// <param name="cardNo">卡号</param>
        /// <param name="modelGroupName">模型分组名</param>
        /// <returns></returns>
        Task<int> CountTimesByGroupNameAsync(string cardNo, string modelGroupName, DateTime? date);
    }
}
