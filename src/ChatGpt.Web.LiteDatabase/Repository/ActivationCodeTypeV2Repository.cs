using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatGpt.Web.Entity.ActivationCodeSys;
using ChatGpt.Web.IRepository.ActivationCodeSys;

namespace ChatGpt.Web.LiteDatabase.Repository
{
    /// <summary>
    /// 卡密类型 仓储实现
    /// </summary>
    public class ActivationCodeTypeV2Repository : IActivationCodeTypeV2Repository
    {
        private readonly LiteDB.LiteDatabase _liteDatabase;
        public ActivationCodeTypeV2Repository(LiteDB.LiteDatabase liteDatabase)
        {
            _liteDatabase = liteDatabase;
        }


        /// <summary>
        /// 表名
        /// </summary>
        private static readonly string TableName = $"GtpWebNetCore_{nameof(ActivationCodeTypeV2)}";

        /// <summary>
        /// 根据Id获取卡密类型
        /// </summary>
        /// <returns></returns>
        public async Task<ActivationCodeTypeV2> GetEntityByIdAsync(long codeTypeId)
        {
            var col = _liteDatabase.GetCollection<ActivationCodeTypeV2>(TableName);
            await Task.CompletedTask;
            return col.FindOne(a => a.Id == codeTypeId);
        }

        /// <summary>
        /// 创建卡密类型
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CreateAsync(List<ActivationCodeTypeV2> entities)
        {
            entities.ForEach(item =>
            {
                item.CreatedTime = DateTime.Now;
            });

            var col = _liteDatabase.GetCollection<ActivationCodeTypeV2>(TableName);
            col.Insert(entities);

            await Task.CompletedTask;
            return true;
        }

        /// <summary>
        ///  获取所有卡密类型
        /// </summary>
        /// <returns></returns>
        public async Task<List<ActivationCodeTypeV2>> GetAllActivationCodeTypeAsync()
        {
            await Task.CompletedTask;
            var col = _liteDatabase.GetCollection<ActivationCodeTypeV2>(TableName);
            return col.FindAll().ToList();
        }

        /// <summary>
        /// 清空卡类型
        /// </summary>
        /// <returns></returns>
        public async Task<bool> DeleteAllAsync()
        {
            await Task.CompletedTask;
            var col = _liteDatabase.GetCollection<ActivationCodeTypeV2>(TableName);
            return col.DeleteAll() > 0;
        }

        /// <summary>
        /// 检查名称是否存在
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CheckNameAsync(string name)
        {
            await Task.CompletedTask;
            var col = _liteDatabase.GetCollection<ActivationCodeTypeV2>(TableName);
            return col.FindOne(a => a.CodeName == name) != null;
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <returns></returns>
        public async Task<bool> UpdateAsync(ActivationCodeTypeV2 entity)
        {
            await Task.CompletedTask;
            var col = _liteDatabase.GetCollection<ActivationCodeTypeV2>(TableName);
            return col.Update(entity);
        }
    }
}
