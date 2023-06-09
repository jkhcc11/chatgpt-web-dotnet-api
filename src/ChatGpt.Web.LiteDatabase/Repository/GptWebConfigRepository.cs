﻿using System.Collections.Generic;
using System.Threading.Tasks;
using ChatGpt.Web.Entity;
using ChatGpt.Web.IRepository;

namespace ChatGpt.Web.LiteDatabase.Repository
{
    /// <summary>
    /// 站点配置 仓储实现
    /// </summary>
    public class GptWebConfigRepository : BaseLiteDatabaseRepository<GptWebConfig, long>, IGptWebConfigRepository
    {
        public GptWebConfigRepository(LiteDB.LiteDatabase liteDatabase) : base(liteDatabase)
        {
        }

        /// <summary>
        /// 获取所有站点配置
        /// </summary>
        /// <returns></returns>
        public async Task<List<GptWebConfig>> GetAllConfigAsync()
        {
            var query = DbCollection.Query();
            await Task.CompletedTask;
            return query.ToList();
        }

    }
}
