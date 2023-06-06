using System.Collections.Generic;
using System.Threading.Tasks;
using ChatGpt.Web.Entity.ActivationCodeSys;
using ChatGpt.Web.IRepository.ActivationCodeSys;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace ChatGpt.Web.MongoDB.Repository
{
    /// <summary>
    /// 卡密信息 仓储实现
    /// </summary>
    public class ActivationCodeRepository : BaseMongodbRepository<ActivationCode, long>, IActivationCodeRepository
    {
        public ActivationCodeRepository(GptWebMongodbContext gptWebMongodbContext) : base(gptWebMongodbContext)
        {
        }

        /// <summary>
        /// 卡密号查卡密
        /// </summary>
        /// <returns></returns>
        public async Task<ActivationCode?> GetActivationCodeByCardNoAsync(string cardNo)
        {
            return await DbCollection.Find(a => a.CardNo == cardNo).FirstOrDefaultAsync();
        }

        /// <summary>
        ///  根据类型获取卡密
        /// </summary>
        /// <param name="codeTypeId">卡密类型ID</param>
        /// <returns></returns>
        public async Task<List<ActivationCode>> QueryActivationCodeByTypeAsync(long? codeTypeId)
        {
            var query = DbCollection.AsQueryable();
            if (codeTypeId.HasValue)
            {
                query = query.Where(a => a.CodyTypeId == codeTypeId.Value);
            }

            return await query.ToListAsync();
        }
    }
}
