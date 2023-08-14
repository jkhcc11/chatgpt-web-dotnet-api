using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatGpt.Web.BaseInterface
{
    /// <summary>
    /// 通用仓储 接口
    /// </summary>
    public interface IBaseRepository<TEntity, in TKey>
    where TEntity : BaseEntity<TKey> where TKey : struct
    {
        /// <summary>
        /// 根据Id获取实体
        /// </summary>
        /// <returns></returns>
        Task<TEntity?> FirstOrDefaultAsync(TKey keyId);

        /// <summary>
        /// 根据Id获取实体
        /// </summary>
        /// <returns></returns>
        Task<TEntity> GetEntityByIdAsync(TKey keyId);

        Task<bool> CreateAsync(TEntity entity);
        /// <summary>
        /// 创建
        /// </summary>
        /// <returns></returns>
        Task<bool> CreateAsync(List<TEntity> entities);

        /// <summary>
        /// 更新
        /// </summary>
        /// <returns></returns>
        Task<bool> UpdateAsync(TEntity entity);

        /// <summary>
        /// 删除
        /// </summary>
        /// <returns></returns>
        Task<bool> DeleteAsync(TEntity entity);

        /// <summary>
        /// 获取所有
        /// </summary>
        /// <returns></returns>
        Task<IReadOnlyList<TEntity>> GetAllListAsync();

        /// <summary>
        /// 分页查询
        /// </summary>
        /// <returns></returns>
        Task<QueryPageDto<TEntity>> QueryPageListAsync(IQueryable<TEntity> query, int page, int pageSize);

        /// <summary>
        /// 获取Queryable
        /// </summary>
        /// <returns></returns>
        Task<IQueryable<TEntity>> GetQueryableAsync();
    }
}
