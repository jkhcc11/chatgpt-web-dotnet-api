namespace ChatGpt.Web.Dto.Inputs
{
    /// <summary>
    /// 流式返回聊天内容
    /// </summary>
    public class ChatProcessInput
    {
        /// <summary>
        /// 内容
        /// </summary>
        public string Prompt { get; set; } = "";

        /// <summary>
        /// 配置
        /// </summary>
        public ChatProcessOption Options { get; set; } = new ChatProcessOption();

        /// <summary>
        /// 默认系统信息
        /// </summary>
        public string SystemMessage { get; set; } =
            "You are ChatGPT, a large language model trained by OpenAI. Answer as concisely as possible.";
    }

    public class ChatProcessOption
    {
        /// <summary>
        /// 父信息ID
        /// </summary>
        public string ParentMessageId { get; set; } = "";
    }
}
