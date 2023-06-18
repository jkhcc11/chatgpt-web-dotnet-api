using System;
using System.Threading.Tasks;
using ChatGpt.Web.Entity.ActivationCodeSys;
using ChatGpt.Web.IRepository.ActivationCodeSys;

namespace ChatGpt.Web.LiteDatabase.Repository
{
    /// <summary>
    /// 按次卡密记录 仓储实现
    /// </summary>
    public class PerUseActivationCodeRecordRepository : BaseLiteDatabaseRepository<PerUseActivationCodeRecord, long>, IPerUseActivationCodeRecordRepository
    {
        public PerUseActivationCodeRecordRepository(LiteDB.LiteDatabase liteDatabase) : base(liteDatabase)
        {
        }

        /// <summary>
        /// 根据日期模型分组统计次数
        /// </summary>
        /// <param name="date">日期 为空查所有</param>
        /// <param name="cardNo">卡号</param>
        /// <param name="modelGroupName">模型分组名</param>
        /// <returns></returns>
        public async Task<int> CountTimesByGroupNameAsync(string cardNo
            , string modelGroupName, DateTime? date)
        {
            var query = DbCollection.Query()
                .Where(a => a.CreatedTime.Date == date &&
                            a.CardNo == cardNo &&
                            a.ModelGroupName == modelGroupName);
            if (date.HasValue)
            {
                query = query.Where(a => a.CreatedTime.Date == date);
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
            var query = DbCollection.Query()
                .Where(a => a.CreatedTime.Date == date &&
                            a.CardNo == cardNo &&
                            a.ModelId == modelId);
            await Task.CompletedTask;
            return query.Count();
        }

    }
}
