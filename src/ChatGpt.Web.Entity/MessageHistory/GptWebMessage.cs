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
        /// <param name="activationCode">卡密</param>
        public GptWebMessage(long id, string msg,
            MsgType msgType, long conversationId, string activationCode) : base(id)
        {
            Msg = msg;
            MsgType = msgType;
            ConversationId = conversationId;
            ActivationCode = activationCode;
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

        /// <summary>
        /// 消耗Tokens
        /// </summary>
        public int Tokens { get; set; }

        /// <summary>
        /// 卡密
        /// </summary>
        public string ActivationCode { get; set; }

        /// <summary>
        /// 响应时长(毫秒)
        /// </summary>
        /// <remarks>
        /// 总响应时长(回复内容才有)
        /// </remarks>
        public long ResponseDuration { get; set; }
    }
}
