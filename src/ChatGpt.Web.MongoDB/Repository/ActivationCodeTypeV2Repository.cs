using System.Collections.Generic;
using System.Threading.Tasks;
using ChatGpt.Web.Entity.ActivationCodeSys;
using ChatGpt.Web.IRepository.ActivationCodeSys;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace ChatGpt.Web.MongoDB.Repository
{
    /// <summary>
    /// 卡密类型 仓储实现
    /// </summary>
    public class ActivationCodeTypeV2Repository : BaseMongodbRepository<ActivationCodeTypeV2, long>, IActivationCodeTypeV2Repository
    {
        public ActivationCodeTypeV2Repository(GptWebMongodbContext gptWebMongodbContext) : base(gptWebMongodbContext)
        {
        }

        /// <summary>
        ///  获取所有卡密类型
        /// </summary>
        /// <returns></returns>
        public async Task<List<ActivationCodeTypeV2>> GetAllActivationCodeTypeAsync()
        {
            return await DbCollection.AsQueryable().ToListAsync();
        }

        /// <summary>
        /// 清空卡类型
        /// </summary>
        /// <returns></returns>
        public async Task<bool> DeleteAllAsync()
        {
            await DbCollection.DeleteManyAsync(a => a.Id > 0);
            return true;
        }

        /// <summary>
        /// 检查名称是否存在
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CheckNameAsync(string name)
        {
            return await DbCollection.AsQueryable().AnyAsync(a => a.CodeName == name);
        }

    }
}
