using System.Collections.Generic;

namespace ChatGpt.Web.BaseInterface.Options
{
    /// <summary>
    /// 配置
    /// </summary>
    public class ChatGptWebConfig
    {
        /// <summary>
        /// OpenAi BaseHost
        /// </summary>
        /// <remarks>
        /// 反代ApiHost
        /// </remarks>
        public string? OpenAiBaseHost { get; set; }

        /// <summary>
        /// ApiKey多个
        /// </summary>
        public List<string> ApiKeys { get; set; } = new List<string>();

        /// <summary>
        /// Api超时时间(毫秒)
        /// </summary>
        public int ApiTimeoutMilliseconds { get; set; } = 30000;

        /// <summary>
        /// 流式停止标识
        /// </summary>
        public string StopFlag { get; set; } = "[DONE]";
    }
}
