using System.Collections.Generic;
using System.Threading.Tasks;
using ChatGpt.Web.Entity.MessageHistory;
using ChatGpt.Web.IRepository.MessageHistory;
using MongoDB.Driver;

namespace ChatGpt.Web.MongoDB.Repository
{
    /// <summary>
    /// 消息 仓储实现
    /// </summary>
    public class GptWebMessageRepository : BaseMongodbRepository<GptWebMessage, long>, IGptWebMessageRepository
    {
        public GptWebMessageRepository(GptWebMongodbContext gptWebMongodbContext) : base(gptWebMongodbContext)
        {
        }

        /// <summary>
        /// 根据会话Id查询消息列表
        /// </summary>
        /// <param name="conversationId">会话Id</param>
        /// <returns></returns>
        public async Task<List<GptWebMessage>> QueryMsgByConversationIdAsync(long conversationId)
        {
            return await DbCollection.Find(a => a.ConversationId == conversationId)
                .ToListAsync();
        }

        /// <summary>
        /// 根据Gpt消息Id查询消息
        /// </summary>
        /// <param name="gptId">gpt消息Id</param>
        /// <returns></returns>
        public async Task<GptWebMessage?> GetMessageByParentMsgIdAsync(string gptId)
        {
            return await DbCollection.Find(a => a.GptMsgId == gptId).FirstOrDefaultAsync();
        }


    }
}
