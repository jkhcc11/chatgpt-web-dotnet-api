namespace ChatGpt.Web.Dto.Inputs
{
    /// <summary>
    /// 创建配置
    /// </summary>
    public class CreateGptWebConfigInput
    {
        /// <summary>
        /// 配置
        /// </summary>
        public CreateGptWebConfigInput(string description, string homeBtnHtml)
        {
            Description = description;
            HomeBtnHtml = homeBtnHtml;
        }

        /// <summary>
        /// 子域名Host
        /// </summary>
        /// <remarks>
        /// 为空时默认
        /// </remarks>
        public string? SubDomainHost { get; set; }

        /// <summary>
        /// 描述Html
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 首页卡密Html
        /// </summary>
        public string HomeBtnHtml { get; set; }
    }
}
