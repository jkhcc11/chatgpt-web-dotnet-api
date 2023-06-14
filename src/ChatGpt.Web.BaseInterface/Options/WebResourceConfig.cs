using System.Collections.Generic;

namespace ChatGpt.Web.BaseInterface.Options
{
    /// <summary>
    /// Web资源配置
    /// </summary>
    public class WebResourceConfig
    {
        /// <summary>
        /// 头像
        /// </summary>
        public string Avatar { get; set; } = "";

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// 首页按钮Html
        /// </summary>
        public string HomeBtnHtml { get; set; } = "";

        /// <summary>
        /// 加微信备注
        /// </summary>
        public string Wxremark { get; set; } = "";

        /// <summary>
        /// 免费卡密
        /// </summary>
        public string FreeCode { get; set; } = "";

        /// <summary>
        /// gpt4卡密
        /// </summary>
        public string FreeCode4 { get; set; } = "";

        /// <summary>
        /// 每天免费次数gpt4
        /// </summary>
        public int EveryDayFreeTimes4 { get; set; } = 1;

        /// <summary>
        /// 每天免费次数
        /// </summary>
        public int EveryDayFreeTimes { get; set; } = 1;

        /// <summary>
        /// 微信图片
        /// </summary>
        public string Wximg { get; set; } = "";

        /// <summary>
        /// 支持模型
        /// </summary>
        public List<NSelectItem> SupportModel { get; set; } = new List<NSelectItem>();
    }

    public class NSelectItem
    {
        /// <summary>
        /// 显示文案
        /// </summary>
        public string Label { get; set; } = "";

        /// <summary>
        /// 值
        /// </summary>
        public string Value { get; set; } = "";

        /// <summary>
        /// 是否禁用
        /// </summary>
        public bool Disabled { get; set; }
    }
}
