﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatGpt.Web.Entity.ActivationCodeSys;
using ChatGpt.Web.IRepository.ActivationCodeSys;

namespace ChatGpt.Web.LiteDatabase.Repository
{
    /// <summary>
    /// 卡密类型 仓储实现
    /// </summary>
    public class ActivationCodeTypeV2Repository : BaseLiteDatabaseRepository<ActivationCodeTypeV2, long>, IActivationCodeTypeV2Repository
    {

        public ActivationCodeTypeV2Repository(LiteDB.LiteDatabase liteDatabase) : base(liteDatabase)
        {
        }

        /// <summary>
        ///  获取所有卡密类型
        /// </summary>
        /// <returns></returns>
        public async Task<List<ActivationCodeTypeV2>> GetAllActivationCodeTypeAsync()
        {
            await Task.CompletedTask;
            return DbCollection.FindAll().ToList();
        }

        /// <summary>
        /// 清空卡类型
        /// </summary>
        /// <returns></returns>
        public async Task<bool> DeleteAllAsync()
        {
            await Task.CompletedTask;
            return DbCollection.DeleteAll() > 0;
        }

        /// <summary>
        /// 检查名称是否存在
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CheckNameAsync(string name)
        {
            await Task.CompletedTask;
            return DbCollection.FindOne(a => a.CodeName == name) != null;
        }

    }
}
