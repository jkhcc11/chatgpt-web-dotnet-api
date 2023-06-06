using System.Collections.Generic;
using System.Threading.Tasks;
using ChatGpt.Web.BaseInterface;
using ChatGpt.Web.Entity.MessageHistory;

namespace ChatGpt.Web.IRepository.MessageHistory
{
    /// <summary>
    /// 消息 仓储接口
    /// </summary>
    public interface IGptWebMessageRepository : IBaseRepository<GptWebMessage, long>
    {
        /// <summary>
        /// 根据会话Id查询消息列表
        /// </summary>
        /// <param name="conversationId">会话Id</param>
        /// <returns></returns>
        Task<List<GptWebMessage>> QueryMsgByConversationIdAsync(long conversationId);

        /// <summary>
        /// 根据Gpt消息Id查询消息
        /// </summary>
        /// <param name="gptId">gpt消息Id</param>
        /// <returns></returns>
        Task<GptWebMessage?> GetMessageByParentMsgIdAsync(string gptId);
    }
}
