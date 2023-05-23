using System;
using System.Threading.Tasks;
using ChatGpt.Web.Entity.ActivationCodeSys;

namespace ChatGpt.Web.IRepository.ActivationCodeSys
{
    /// <summary>
    /// 按次卡密记录 仓储接口
    /// </summary>
    public interface IPerUseActivationCodeRecordRepository
    {
        /// <summary>
        /// 创建记录
        /// </summary>
        /// <returns></returns>
        Task<bool> CreateAsync(PerUseActivationCodeRecord entity);

        /// <summary>
        /// 根据日期统计次数
        /// </summary>
        /// <param name="date">日期</param>
        /// <param name="cardNo">卡号</param>
        /// <returns></returns>
        Task<int> CountTimesAsync(DateTime date, string cardNo);
    }
}
