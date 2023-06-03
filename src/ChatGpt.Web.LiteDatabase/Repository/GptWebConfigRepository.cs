using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatGpt.Web.Entity;
using ChatGpt.Web.IRepository;

namespace ChatGpt.Web.LiteDatabase.Repository
{
    /// <summary>
    /// 站点配置 仓储实现
    /// </summary>
    public class GptWebConfigRepository : IGptWebConfigRepository
    {
        private readonly LiteDB.LiteDatabase _liteDatabase;
        public GptWebConfigRepository(LiteDB.LiteDatabase liteDatabase)
        {
            _liteDatabase = liteDatabase;
        }

        /// <summary>
        /// 表名
        /// </summary>
        private static readonly string TableName = $"GtpWebNetCore_{nameof(GptWebConfig)}";

        /// <summary>
        /// 创建
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CreateAsync(GptWebConfig entity)
        {
            entity.CreatedTime = DateTime.Now;

            var col = _liteDatabase.GetCollection<GptWebConfig>(TableName);
            col.Insert(entity);

            await Task.CompletedTask;
            return true;
        }

        /// <summary>
        /// 获取所有站点配置
        /// </summary>
        /// <returns></returns>
        public async Task<List<GptWebConfig>> GetAllConfigAsync()
        {
            var col = _liteDatabase.GetCollection<GptWebConfig>(TableName);
            var query = col.Query();
            await Task.CompletedTask;
            return query.ToList();
        }

        /// <summary>
        /// 修改
        /// </summary>
        /// <returns></returns>
        public async Task<bool> UpdateAsync(GptWebConfig entity)
        {
            var col = _liteDatabase.GetCollection<GptWebConfig>(TableName);
            col.Update(entity);
            await Task.CompletedTask;
            return true;
        }
    }
}
