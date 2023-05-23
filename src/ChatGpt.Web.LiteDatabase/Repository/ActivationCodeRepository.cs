using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatGpt.Web.Entity.ActivationCodeSys;
using ChatGpt.Web.Entity.Enums;
using ChatGpt.Web.IRepository.ActivationCodeSys;

namespace ChatGpt.Web.LiteDatabase.Repository
{
    /// <summary>
    /// 卡密信息 仓储实现
    /// </summary>
    public class ActivationCodeRepository : IActivationCodeRepository
    {
        private readonly LiteDB.LiteDatabase _liteDatabase;
        public ActivationCodeRepository(LiteDB.LiteDatabase liteDatabase)
        {
            _liteDatabase = liteDatabase;
        }


        /// <summary>
        /// 表名
        /// </summary>
        private static readonly string TableName = $"GtpWebNetCore_{nameof(ActivationCode)}";

        /// <summary>
        /// 卡密号查卡密
        /// </summary>
        /// <returns></returns>
        public async Task<ActivationCode?> GetActivationCodeByCardNoAsync(string cardNo)
        {
            var col = _liteDatabase.GetCollection<ActivationCode>(TableName);
            await Task.CompletedTask;
            return col.FindOne(a => a.CardNo == cardNo);
        }

        /// <summary>
        /// 创建卡密
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CreateAsync(List<ActivationCode> entities)
        {
            entities.ForEach(item =>
            {
                item.CreatedTime = DateTime.Now;
            });

            var col = _liteDatabase.GetCollection<ActivationCode>(TableName);
            col.Insert(entities);

            await Task.CompletedTask;
            return true;
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <returns></returns>
        public async Task<bool> UpdateAsync(ActivationCode entity)
        {
            var col = _liteDatabase.GetCollection<ActivationCode>(TableName);
            col.Update(entity);
            await Task.CompletedTask;
            return true;
        }

        /// <summary>
        ///  根据类型获取卡密
        /// </summary>
        /// <param name="codeType">卡密类型</param>
        /// <returns></returns>
        public async Task<List<ActivationCode>> QueryActivationCodeByTypeAsync(ActivationCodeType? codeType)
        {
            var col = _liteDatabase.GetCollection<ActivationCode>(TableName);
            var query = col.Query();
            if (codeType.HasValue)
            {
                query = query.Where(a => a.CodeType == codeType.Value);
            }

            await Task.CompletedTask;
            return query.ToList();
        }
    }
}
