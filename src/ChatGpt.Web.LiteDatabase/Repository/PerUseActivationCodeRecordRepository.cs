﻿using System;
using System.Threading.Tasks;
using ChatGpt.Web.Entity.ActivationCodeSys;
using ChatGpt.Web.IRepository.ActivationCodeSys;

namespace ChatGpt.Web.LiteDatabase.Repository
{
    /// <summary>
    /// 按次卡密记录 仓储实现
    /// </summary>
    public class PerUseActivationCodeRecordRepository : IPerUseActivationCodeRecordRepository
    {
        private readonly LiteDB.LiteDatabase _liteDatabase;

        /// <summary>
        /// 表名
        /// </summary>
        private static readonly string TableName = $"GtpWebNetCore_{nameof(PerUseActivationCodeRecord)}";

        public PerUseActivationCodeRecordRepository(LiteDB.LiteDatabase liteDatabase)
        {
            _liteDatabase = liteDatabase;
        }

        /// <summary>
        /// 创建记录
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CreateAsync(PerUseActivationCodeRecord entity)
        {
            entity.CreatedTime = DateTime.Now;
            var col = _liteDatabase.GetCollection<PerUseActivationCodeRecord>(TableName);
            col.Insert(entity);
            await Task.CompletedTask;
            return true;
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
            var col = _liteDatabase.GetCollection<PerUseActivationCodeRecord>(TableName);
            var query = col.Query()
                .Where(a => a.CreatedTime.Date == date &&
                            a.CardNo == cardNo &&
                            a.ModelGroupName == modelGroupName);
            await Task.CompletedTask;
            return query.Count();
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
            var col = _liteDatabase.GetCollection<PerUseActivationCodeRecord>(TableName);
            var query = col.Query()
                .Where(a => a.CreatedTime.Date == date &&
                            a.CardNo == cardNo &&
                            a.ModelId == modelId);
            await Task.CompletedTask;
            return query.Count();
        }
    }
}
