using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatGpt.Web.BaseInterface;
using MongoDB.Driver;

namespace ChatGpt.Web.MongoDB
{
    /// <summary>
    /// Mongodb通用仓储 实现
    /// </summary>
    /// <typeparam name="TEntity">实体类 继承<see cref="BaseEntity&lt;TKey&gt;"/></typeparam>
    /// <typeparam name="TKey">主键</typeparam>
    public abstract class BaseMongodbRepository<TEntity, TKey> : IBaseRepository<TEntity, TKey>
        where TEntity : BaseEntity<TKey> where TKey : struct
    {
        protected readonly string CollectionName;
        protected readonly IMongoCollection<TEntity> DbCollection;

        protected BaseMongodbRepository(GptWebMongodbContext gptWebContext)
        {
            CollectionName = $"GtpWebNetCore_{typeof(TEntity).Name}";
            DbCollection = gptWebContext.Database.GetCollection<TEntity>(CollectionName);
        }

        /// <summary>
        /// 根据Id获取实体
        /// </summary>
        /// <returns></returns>
        public async Task<TEntity?> FirstOrDefaultAsync(TKey keyId)
        {
            return await DbCollection.Find(a => Equals(a.Id, keyId)).FirstOrDefaultAsync();
        }

        /// <summary>
        /// 根据Id获取实体
        /// </summary>
        /// <returns></returns>
        public async Task<TEntity> GetEntityByIdAsync(TKey keyId)
        {
            return await DbCollection.Find(a => Equals(a.Id, keyId)).FirstAsync();
        }

        public async Task<bool> CreateAsync(TEntity entity)
        {
            entity.CreatedTime = DateTime.Now;
            await DbCollection.InsertOneAsync(entity);
            return true;
        }

        /// <summary>
        /// 创建
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CreateAsync(List<TEntity> entities)
        {
            entities.ForEach(item =>
            {
                item.CreatedTime = DateTime.Now;
            });

            await DbCollection.InsertManyAsync(entities);
            return true;
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <returns></returns>
        public async Task<bool> UpdateAsync(TEntity entity)
        {
            entity.ModifyTime = DateTime.Now;
            await DbCollection.ReplaceOneAsync(a => Equals(a.Id, entity.Id), entity);
            return true;
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <returns></returns>
        public async Task<bool> DeleteAsync(TEntity entity)
        {
            await DbCollection.DeleteOneAsync(a => Equals(a.Id, entity.Id));
            return true;
        }
    }
}
