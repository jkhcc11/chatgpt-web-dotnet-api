using ChatGpt.Web.Entity.ActivationCodeSys;
using ChatGpt.Web.Entity.Enums;
using System.Collections.Generic;

namespace ChatGpt.Web.Dto.Dtos.ActivationCodeAdmin
{
    /// <summary>
    /// 分页获取卡密类型
    /// </summary>
    public class QueryPageCodeTypeDto
    {
        /// <summary>
        /// 卡密名称
        /// </summary>
        public string CodeName { get; set; } = "";

        /// <summary>
        /// 支持聊天系统类型
        /// </summary>
        public SupportChatSystemType SupportChatSystemType { get; set; }

        /// <summary>
        /// 有效天数
        /// </summary>
        public int ValidDays { get; set; }

        /// <summary>
        /// 是否每天重置请求次数
        /// </summary>
        /// <remarks>
        ///  1、True每天按次检查 <see cref="MaxCountItems"/>  <br/>
        ///  2、False 在有效期内检查<see cref="MaxCountItems"/>
        /// </remarks>
        public bool IsEveryDayResetCount { get; set; }

        /// <summary>
        ///  最大请求次数
        /// </summary>
        /// <remarks>
        /// 没有配置的模型，默认不限制
        /// </remarks>
        public List<MaxCountItem> MaxCountItems { get; set; } = new List<MaxCountItem>();

        /// <summary>
        /// 支持模型列表
        /// </summary>
        public List<SupportModeItem> SupportModelItems { get; set; } = new List<SupportModeItem>();
    }
}
