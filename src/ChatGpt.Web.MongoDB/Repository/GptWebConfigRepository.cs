using System.Collections.Generic;
using System.Threading.Tasks;
using ChatGpt.Web.Entity;
using ChatGpt.Web.IRepository;
using MongoDB.Driver;

namespace ChatGpt.Web.MongoDB.Repository
{
    /// <summary>
    /// 站点配置 仓储实现
    /// </summary>
    public class GptWebConfigRepository : BaseMongodbRepository<GptWebConfig, long>, IGptWebConfigRepository
    {
        public GptWebConfigRepository(GptWebMongodbContext gptWebMongodbContext) : base(gptWebMongodbContext)
        {
        }

        /// <summary>
        /// 获取所有站点配置
        /// </summary>
        /// <returns></returns>
        public async Task<List<GptWebConfig>> GetAllConfigAsync()
        {
            return await DbCollection.AsQueryable().ToListAsync();
        }


    }
}
