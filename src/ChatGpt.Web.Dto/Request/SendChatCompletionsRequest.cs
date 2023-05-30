using System.Collections.Generic;
using Newtonsoft.Json;

namespace ChatGpt.Web.Dto.Request
{
    /// <summary>
    /// 发起聊天
    /// </summary>
    public class SendChatCompletionsRequest
    {
        public SendChatCompletionsRequest(List<SendChatCompletionsMessageItem> messages)
        {
            Messages = messages;
        }

        /// <summary>
        /// 聊天完成时生成的最大令牌数(输出的总长度tokens)
        /// </summary>
        [JsonProperty("max_tokens")]
        public int MaxTokens { get; set; } = 10;

        /// <summary>
        /// 要使用的模型的 ID
        /// </summary>
        [JsonProperty("model")]
        public string Model { get; set; } = "gpt-3.5-turbo";

        /// <summary>
        /// 0 和 2 之间。
        /// 较高的值（如 0.8）将使输出更加随机，而较低的值（如 0.2）将使输出更加集中和确定
        /// </summary>
        [JsonProperty("temperature")]
        public decimal Temperature { get; set; } = 0.8M;

        /// <summary>
        /// 对应Temperature的质量
        /// 改变这个或temperature但不是两者
        /// </summary>
        [JsonProperty("top_p")]
        public decimal TopP { get; set; } = 1;

        /// <summary>
        /// -2.0 和 2.0 之间的数字。正值会根据到目前为止是否出现在文本中来惩罚新标记，从而增加模型谈论新主题的可能性
        /// </summary>
        [JsonProperty("presence_penalty")]
        public int PresencePenalty { get; set; } = 1;

        /// <summary>
        /// 是否流式
        /// </summary>
        [JsonProperty("stream")]
        public bool Stream { get; set; }

        /// <summary>
        /// 消息列表
        /// </summary>
        [JsonProperty("messages")]
        public List<SendChatCompletionsMessageItem> Messages { get; protected set; }
    }

    /// <summary>
    /// 消息Item
    /// </summary>
    public class SendChatCompletionsMessageItem
    {
        public SendChatCompletionsMessageItem(string role, string content)
        {
            Role = role;
            Content = content;
        }

        /// <summary>
        /// 角色
        /// </summary>
        [JsonProperty("role")]
        public string Role { get; set; }

        /// <summary>
        /// 内容
        /// </summary>
        [JsonProperty("content")]
        public string Content { get; set; }
    }
}
