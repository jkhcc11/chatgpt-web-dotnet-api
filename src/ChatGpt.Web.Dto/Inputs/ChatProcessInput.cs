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
        
        /// <summary>
        /// Api模型
        /// </summary>
        public string ApiModel { get; set; } = "gpt-3.5-turbo";

        /// <summary>
        /// 0 和 2 之间。
        /// 较高的值（如 0.8）将使输出更加随机，而较低的值（如 0.2）将使输出更加集中和确定
        /// </summary>
        public decimal Temperature { get; set; } = 0.8M;

        /// <summary>
        /// 对应Temperature的质量
        /// 改变这个或temperature但不是两者
        /// </summary>
        public decimal TopP { get; set; } = 1;
    }

    public class ChatProcessOption
    {
        /// <summary>
        /// 父信息ID
        /// </summary>
        public string ParentMessageId { get; set; } = "";
    }
}
