using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatGpt.Web.Entity.MessageHistory;
using ChatGpt.Web.IRepository.MessageHistory;

namespace ChatGpt.Web.LiteDatabase.Repository
{
    /// <summary>
    /// 消息 仓储实现
    /// </summary>
    public class GptWebMessageRepository : IGptWebMessageRepository
    {
        private readonly LogLiteDatabase _liteDatabase;
        public GptWebMessageRepository(LogLiteDatabase liteDatabase)
        {
            _liteDatabase = liteDatabase;
        }


        /// <summary>
        /// 表名
        /// </summary>
        private static readonly string TableName = $"GtpWebNetCore_{nameof(GptWebMessage)}";

        /// <summary>
        /// 创建
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CreateAsync(GptWebMessage entity)
        {
            entity.CreatedTime = DateTime.Now;
            var col = _liteDatabase.GetCollection<GptWebMessage>(TableName);
            col.Insert(entity);

            await Task.CompletedTask;
            return true;
        }

        /// <summary>
        /// 批量创建
        /// </summary>
        /// <returns></returns>
        public async Task<bool> BatchCreateAsync(List<GptWebMessage> entities)
        {
            entities.ForEach(item =>
            {
                item.CreatedTime = DateTime.Now;
            });

            var col = _liteDatabase.GetCollection<GptWebMessage>(TableName);
            col.Insert(entities);

            await Task.CompletedTask;
            return true;
        }

        /// <summary>
        /// 根据会话Id查询消息列表
        /// </summary>
        /// <param name="conversationId">会话Id</param>
        /// <returns></returns>
        public async Task<List<GptWebMessage>> QueryMsgByConversationIdAsync(long conversationId)
        {
            var col = _liteDatabase.GetCollection<GptWebMessage>(TableName);
            await Task.CompletedTask;
            return col.Find(a => a.ConversationId == conversationId).ToList();
        }

        /// <summary>
        /// 根据Gpt消息Id查询消息
        /// </summary>
        /// <param name="gptId">gpt消息Id</param>
        /// <returns></returns>
        public async Task<GptWebMessage?> GetMessageByParentMsgIdAsync(string gptId)
        {
            var col = _liteDatabase.GetCollection<GptWebMessage>(TableName);
            await Task.CompletedTask;
            return col.FindOne(a => a.GptMsgId == gptId);
        }
    }
}
