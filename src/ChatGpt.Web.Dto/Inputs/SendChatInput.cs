using System.Collections.Generic;
using ChatGpt.Web.Dto.Request;

namespace ChatGpt.Web.Dto.Inputs
{
    /// <summary>
    /// 发起聊天 Input
    /// </summary>
    public class SendChatInput : SendChatCompletionsRequest
    {
        public SendChatInput(string apiKey, List<SendChatCompletionsMessageItem> messages) : base(messages)
        {
            ApiKey = apiKey;
        }

        /// <summary>
        /// ApiKey
        /// </summary>
        public string ApiKey { get; set; }

    }
}
