using ChatGpt.Web.BaseInterface;

namespace ChatGpt.Web.Entity
{
    /// <summary>
    /// 站点配置
    /// </summary>
    public class GptWebConfig : BaseEntity<long>
    {
        public GptWebConfig(long id) : base(id)
        {
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
        public string? Description { get; set; }

        /// <summary>
        /// 首页卡密Html
        /// </summary>
        public string? HomeBtnHtml { get; set; }
    }
}
