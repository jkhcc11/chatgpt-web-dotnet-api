using ChatGpt.Web.BaseInterface;
using ChatGpt.Web.Entity.Enums;

namespace ChatGpt.Web.Entity.MessageHistory
{
    /// <summary>
    /// 消息
    /// </summary>
    public class GptWebMessage : BaseEntity<long>
    {
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="id"></param>
        /// <param name="msg">消息</param>
        /// <param name="msgType">消息类型</param>
        /// <param name="conversationId">会话Id</param>
        public GptWebMessage(long id, string msg,
            MsgType msgType, long conversationId) : base(id)
        {
            Msg = msg;
            MsgType = msgType;
            ConversationId = conversationId;
        }

        /// <summary>
        /// 消息
        /// </summary>
        public string Msg { get; set; }

        /// <summary>
        /// Gpt消息Id
        /// </summary>
        /// <remarks>
        /// 前端只会传这个值
        /// </remarks>
        public string? GptMsgId { get; set; }

        /// <summary>
        /// 父消息主键
        /// </summary>
        public long? ParentId { get; set; }

        /// <summary>
        /// 消息类型
        /// </summary>
        public MsgType MsgType { get; set; }

        /// <summary>
        /// 会话Id
        /// </summary>
        public long ConversationId { get; set; }

        /// <summary>
        /// Gpt请求内容
        /// </summary>
        public string? GtpRequest { get; set; }

        /// <summary>
        /// Gpt返回详情
        /// </summary>
        public string? GtpResponse { get; set; }
    }
}
