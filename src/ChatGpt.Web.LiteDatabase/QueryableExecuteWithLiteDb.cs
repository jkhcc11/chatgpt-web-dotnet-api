using ChatGpt.Web.BaseInterface;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatGpt.Web.LiteDatabase
{
    /// <summary>
    /// LiteDb
    /// </summary>
    public class QueryableExecuteWithLiteDb: IQueryableExecute
    {
        /// <summary>
        /// Any
        /// </summary>
        /// <returns></returns>
        public async Task<bool> AnyAsync<TEntity>(IQueryable<TEntity> queryable)
        {
            await Task.CompletedTask;
            return queryable.Any();
        }

        /// <summary>
        /// ToList
        /// </summary>
        /// <returns></returns>
        public async Task<IReadOnlyList<TEntity>> ToListAsync<TEntity>(IQueryable<TEntity> queryable)
        {
            await Task.CompletedTask;
            return queryable.ToList();
        }
    }
}
