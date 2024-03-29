﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ChatGpt.Web.BaseInterface;
using LiteDB;

namespace ChatGpt.Web.LiteDatabase
{
    /// <summary>
    /// LiteDatabase通用仓储 实现
    /// </summary>
    /// <typeparam name="TEntity">实体类 继承<see cref="BaseEntity&lt;TKey&gt;"/></typeparam>
    /// <typeparam name="TKey">主键</typeparam>
    public abstract class BaseLiteDatabaseRepository<TEntity, TKey> : IBaseRepository<TEntity, TKey>
        where TEntity : BaseEntity<TKey> where TKey : struct
    {
        protected readonly LiteDB.LiteDatabase LiteDatabase;
        protected readonly string CollectionName;
        protected readonly ILiteCollection<TEntity> DbCollection;

        protected BaseLiteDatabaseRepository(LiteDB.LiteDatabase liteDatabase)
        {
            LiteDatabase = liteDatabase;
            CollectionName = $"GtpWebNetCore_{typeof(TEntity).Name}";
            DbCollection = LiteDatabase.GetCollection<TEntity>(CollectionName);
        }

        /// <summary>
        /// 根据Id获取实体
        /// </summary>
        /// <returns></returns>
        public virtual async Task<TEntity?> FirstOrDefaultAsync(TKey keyId)
        {
            await Task.CompletedTask;
            return DbCollection.FindById(new BsonValue(keyId));
        }

        /// <summary>
        /// 根据Id获取实体
        /// </summary>
        /// <returns></returns>
        public virtual async Task<TEntity> GetEntityByIdAsync(TKey keyId)
        {
            await Task.CompletedTask;
            return DbCollection.FindById(new BsonValue(keyId));
        }

        public virtual async Task<bool> CreateAsync(TEntity entity)
        {
            await Task.CompletedTask;
            entity.CreatedTime = DateTime.Now;
            DbCollection.Insert(entity);
            return true;
        }

        /// <summary>
        /// 创建
        /// </summary>
        /// <returns></returns>
        public virtual async Task<bool> CreateAsync(List<TEntity> entities)
        {
            entities.ForEach(item =>
            {
                item.CreatedTime = DateTime.Now;
            });

            await Task.CompletedTask;
            return DbCollection.InsertBulk(entities) > 0;
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <returns></returns>
        public virtual async Task<bool> UpdateAsync(TEntity entity)
        {
            await Task.CompletedTask;
            entity.ModifyTime = DateTime.Now;
            return DbCollection.Update(entity);
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <returns></returns>
        public virtual async Task<bool> DeleteAsync(TEntity entity)
        {
            await Task.CompletedTask;
            DbCollection.Delete(new BsonValue(entity.Id));
            //DbCollection.DeleteMany(a => Equals(a.Id, entity.Id));
            return true;
        }

        /// <summary>
        /// 获取所有
        /// </summary>
        /// <returns></returns>
        public virtual async Task<IReadOnlyList<TEntity>> GetAllListAsync()
        {
            await Task.CompletedTask;
            return DbCollection.FindAll().ToList();
        }

        /// <summary>
        /// 分页查询
        /// </summary>
        /// <returns></returns>
        public virtual async Task<QueryPageDto<TEntity>> QueryPageListAsync(IQueryable<TEntity> query, int page, int pageSize)
        {
            var total = query.LongCount();
            var allData = query
                .OrderByDescending(a=>a.CreatedTime)
                .Skip((page - 1) * pageSize)
                .ToList();

            var dbResult = allData
                .Take(pageSize)
                .ToList();

            await Task.CompletedTask;
            return new QueryPageDto<TEntity>()
            {
                Total = total,
                Items = dbResult
            };
        }

        /// <summary>
        /// 获取Queryable
        /// </summary>
        /// <returns></returns>
        public virtual async Task<IQueryable<TEntity>> GetQueryableAsync()
        {
            await Task.CompletedTask;
            return DbCollection.Query().ToEnumerable().AsQueryable();
        }

        /// <summary>
        /// 是否存在
        /// </summary>
        /// <returns></returns>
        public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate)
        {
            await Task.CompletedTask;
            return DbCollection.Query()
                .Where(predicate)
                .Exists();
        }

        /// <summary>
        /// 列表
        /// </summary>
        /// <returns></returns>
        public async Task<IReadOnlyList<TEntity>> ToListAsync()
        {
            await Task.CompletedTask;
            return DbCollection.Query().ToList();
        }
    }
}
