using System;
using System.Threading.Tasks;
using ChatGpt.Web.Entity.ActivationCodeSys;
using ChatGpt.Web.IRepository.ActivationCodeSys;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace ChatGpt.Web.MongoDB.Repository
{
    /// <summary>
    /// 按次卡密记录 仓储实现
    /// </summary>
    public class PerUseActivationCodeRecordRepository : BaseMongodbRepository<PerUseActivationCodeRecord, long>, IPerUseActivationCodeRecordRepository
    {
        public PerUseActivationCodeRecordRepository(GptWebMongodbContext gptWebMongodbContext) : base(gptWebMongodbContext)
        {
        }

        /// <summary>
        /// 根据日期模型分组统计次数
        /// </summary>
        /// <param name="date">日期</param>
        /// <param name="cardNo">卡号</param>
        /// <param name="modelGroupName">模型分组名</param>
        /// <returns></returns>
        public async Task<int> CountTimesByGroupNameAsync(DateTime date, string cardNo
            , string modelGroupName)
        {
            var startTime = date.Date;
            var endTime = Convert.ToDateTime($"{date:yyyy-MM-dd 23:59:59}");

            var query = DbCollection.AsQueryable()
                .Where(a => a.CreatedTime >= startTime &&
                            a.CreatedTime <= endTime &&
                            a.CardNo == cardNo &&
                            a.ModelGroupName == modelGroupName);
            return await query.CountAsync();
        }

        /// <summary>
        /// 根据日期模型Id统计次数
        /// </summary>
        /// <param name="date">日期</param>
        /// <param name="cardNo">卡号</param>
        /// <param name="modelId">模型ID</param>
        /// <returns></returns>
        public async Task<int> CountTimesByModelIdAsync(DateTime date, string cardNo, string modelId)
        {
            var startTime = date.Date;
            var endTime = Convert.ToDateTime($"{date:yyyy-MM-dd 23:59:59}");

            var query = DbCollection.AsQueryable()
                .Where(a => a.CreatedTime >= startTime &&
                            a.CreatedTime <= endTime &&
                            a.CardNo == cardNo &&
                            a.ModelId == modelId);
            return await query.CountAsync();
        }

    }
}
