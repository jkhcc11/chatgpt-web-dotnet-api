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
    public class GptWebMessageRepository : BaseLiteDatabaseRepository<GptWebMessage, long>, IGptWebMessageRepository
    {
        //todo 这里分文件存的log
        private readonly LogLiteDatabase _logLiteDatabase;
        public GptWebMessageRepository(LiteDB.LiteDatabase liteDatabase, LogLiteDatabase logLiteDatabase)
            : base(liteDatabase)
        {
            _logLiteDatabase = logLiteDatabase;
        }

        /// <summary>
        /// 表名
        /// </summary>
        private static readonly string TableName = $"GtpWebNetCore_{nameof(GptWebMessage)}";

        /// <summary>
        /// 创建
        /// </summary>
        /// <returns></returns>
        public override async Task<bool> CreateAsync(GptWebMessage entity)
        {
            entity.CreatedTime = DateTime.Now;
            var col = _logLiteDatabase.GetCollection<GptWebMessage>(TableName);
            col.Insert(entity);

            await Task.CompletedTask;
            return true;
        }

        /// <summary>
        /// 批量创建
        /// </summary>
        /// <returns></returns>
        public override async Task<bool> CreateAsync(List<GptWebMessage> entities)
        {
            entities.ForEach(item =>
            {
                item.CreatedTime = DateTime.Now;
            });

            var col = _logLiteDatabase.GetCollection<GptWebMessage>(TableName);
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
            var col = _logLiteDatabase.GetCollection<GptWebMessage>(TableName);
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
            var col = _logLiteDatabase.GetCollection<GptWebMessage>(TableName);
            await Task.CompletedTask;
            return col.FindOne(a => a.GptMsgId == gptId);
        }

        /// <summary>
        /// 获取所有
        /// </summary>
        /// <returns></returns>
        public override async Task<IReadOnlyList<GptWebMessage>> GetAllListAsync()
        {
            var col = _logLiteDatabase.GetCollection<GptWebMessage>(TableName);
            await Task.CompletedTask;
            return col.FindAll().ToList();
        }
    }
}
