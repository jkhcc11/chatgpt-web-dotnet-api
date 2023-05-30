using System.Collections.Generic;

namespace ChatGpt.Web.BaseInterface.Options
{
    /// <summary>
    /// 配置
    /// </summary>
    public class ChatGptWebConfig
    {
        /// <summary>
        /// ApiKey多个
        /// </summary>
        public List<ApiKeyItem> ApiKeys { get; set; } = new List<ApiKeyItem>();

        /// <summary>
        /// Api超时时间(毫秒)
        /// </summary>
        public int ApiTimeoutMilliseconds { get; set; } = 30000;

        /// <summary>
        /// 流式停止标识
        /// </summary>
        public string StopFlag { get; set; } = "[DONE]";
    }

    /// <summary>
    /// ApiKey Item
    /// </summary>
    public class ApiKeyItem
    {
        /// <summary>
        /// OpenAi BaseHost
        /// </summary>
        /// <remarks>
        /// 反代ApiHost
        /// </remarks>
        public string? OpenAiBaseHost { get; set; }

        /// <summary>
        /// ApiKey
        /// </summary>
        public string ApiKey { get; set; } = "";

        /// <summary>
        /// 组织Id
        /// </summary>
        /// <remarks>
        ///  只有一个组织时 可不用配置
        /// </remarks>
        public string? OrgId { get; set; }

        /// <summary>
        /// 模型分组
        /// </summary>
        public string? ModelGroupName { get; set; }
    }
}
