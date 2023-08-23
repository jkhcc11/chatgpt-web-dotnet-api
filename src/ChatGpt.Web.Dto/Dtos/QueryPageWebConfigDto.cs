using ChatGpt.Web.BaseInterface;
using System.Collections.Generic;

namespace ChatGpt.Web.Dto.Dtos
{
    /// <summary>
    /// 分页获取站点配置
    /// </summary>
    public class QueryPageWebConfigDto: BaseEntityDto<long>
    {
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

        /// <summary>
        /// 默认头像
        /// </summary>
        public string? Avatar { get; set; }

        /// <summary>
        /// 默认站点显示名
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// gpt3卡密
        /// </summary>
        public string? FreeCode3 { get; set; }

        /// <summary>
        /// gpt4卡密
        /// </summary>
        public string? FreeCode4 { get; set; }

        /// <summary>
        /// gpt3免费次数
        /// </summary>
        public int FreeTimesWith3 { get; set; }

        /// <summary>
        /// gpt4免费次数
        /// </summary>
        public int FreeTimesWith4 { get; set; }

        /// <summary>
        /// wx img
        /// </summary>
        public string? Wximg { get; set; }

        /// <summary>
        /// 加微信备注
        /// </summary>
        public string? Wxremark { get; set; }

        /// <summary>
        /// 支持的模型
        /// </summary>
        public List<string>? SupportModel { get; set; }
    }
}
