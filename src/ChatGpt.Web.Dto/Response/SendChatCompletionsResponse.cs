using System.Collections.Generic;
using System.IO;
using ChatGpt.Web.Dto.Request;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ChatGpt.Web.Dto.Response
{
    /// <summary>
    /// 返回内容
    /// </summary>
    public class SendChatCompletionsResponse
    {
        /// <summary>
        /// 返回Stream
        /// </summary>
        [JsonIgnore]
        public Stream? ResponseStream { get; set; }

        /// <summary>
        /// 聊天Id
        /// </summary>
        [JsonProperty("id")]
        public string ChatId { get; set; } = "";

        /// <summary>
        /// ? chat.completion
        /// </summary>
        [JsonProperty("object")]
        public string ObjectStr { get; set; } = "";

        /// <summary>
        /// 创建时间戳
        /// </summary>
        [JsonProperty("created")]
        public int Created { get; set; }

        /// <summary>
        /// 聊天模型
        /// </summary>
        [JsonProperty("model")]
        public string Model { get; set; } = "";

        /// <summary>
        /// Tokens消耗
        /// </summary>
        [JsonProperty("usage")]
        public TokensUsage TokensUsage { get; set; } = new TokensUsage();

        /// <summary>
        /// 回复内容数组
        /// </summary>
        [JsonProperty("choices")]
        public List<ChoicesItem> Choices { get; set; } = new List<ChoicesItem>();
    }

    /// <summary>
    /// Tokens消耗详情
    /// </summary>
    public class TokensUsage
    {
        /// <summary>
        /// 提示令牌消耗
        /// </summary>
        [JsonProperty("prompt_tokens")]
        public int PromptTokens { get; set; }

        /// <summary>
        /// 完成消耗
        /// </summary>
        [JsonProperty("completion_tokens")]
        public int CompletionTokens { get; set; }

        /// <summary>
        /// 总消耗
        /// </summary>
        [JsonProperty("total_tokens")]
        public int TotalTokens { get; set; }
    }

    /// <summary>
    /// 回复内容Item
    /// </summary>
    public class ChoicesItem
    {
        /// <summary>
        /// 差异|变化量（流式返回使用）
        /// </summary>
        /// <remarks>
        ///  {"role":"assistant"} 或
        ///   {"content":"你"}
        /// </remarks>
        [JsonProperty("delta")]
        public JObject? DeltaObj { get; set; }

        /// <summary>
        /// 消息实体类
        /// </summary>
        [JsonProperty("message")]
        public SendChatCompletionsMessageItem? Message { get; set; }

        /// <summary>
        /// 结束原因 默认是  stop
        /// </summary>
        [JsonProperty("finish_reason")]
        public string FinishReason { get; set; } = "stop";

        /// <summary>
        /// 序号
        /// </summary>
        [JsonProperty("index")]
        public int Index { get; set; }

    }

}
