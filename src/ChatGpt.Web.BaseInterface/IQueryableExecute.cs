using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatGpt.Web.BaseInterface
{
    /// <summary>
    /// 通用IQueryable 执行
    /// </summary>
    public interface IQueryableExecute
    {
        /// <summary>
        /// Any
        /// </summary>
        /// <returns></returns>
        Task<bool> AnyAsync<TEntity>(IQueryable<TEntity> queryable);

        /// <summary>
        /// ToList
        /// </summary>
        /// <returns></returns>
        Task<IReadOnlyList<TEntity>> ToListAsync<TEntity>(IQueryable<TEntity> queryable);
    }
}
