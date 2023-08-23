using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ChatGpt.Web.Dto.Inputs.ActivationCodeAdmin
{
    /// <summary>
    /// 创建/修改卡密类型
    /// </summary>
    public class CreateAndUpdateCodeTypeInput
    {
        /// <summary>
        /// 主键
        /// </summary>
        public long? Id { get; set; }

        /// <summary>
        /// 卡类型名
        /// </summary>
        [Required]
        public string CardTypeName { get; set; } = "";

        /// <summary>
        /// 支持模型组  gpt3|gpt4
        /// </summary>
        [Required]
        public List<string> SupportModelGroupNameItems { get; set; } = new List<string>();

        /// <summary>
        /// 有效天数
        /// </summary>
        public int ValidDays { get; set; }

        /// <summary>
        /// 是否每天重置请求次数
        /// </summary>
        /// <remarks>
        ///  每天有请求次数限制
        /// </remarks>
        public bool IsEveryDayResetCount { get; set; }

        /// <summary>
        /// 限制Items
        /// </summary>
        [Required]
        public List<LimitItem> LimitItems { get; set; } = new List<LimitItem>();
    }

    /// <summary>
    /// 限制Item
    /// </summary>
    public class LimitItem
    {
        /// <summary>
        /// 模型组名
        /// </summary>
        [Required]
        public string SupportModelGroupName { get; set; } = "";

        /// <summary>
        /// 每天限制次数
        /// </summary>
        public int EveryDayTimes { get; set; }

        /// <summary>
        /// 最大携带历史
        /// </summary>
        public int? MaxHistoryCount { get; set; }

        /// <summary>
        /// 最大请求Tokens
        /// </summary>
        public int? MaxRequestTokens { get; set; }

        /// <summary>
        /// 最大返回Tokens
        /// </summary>
        public int? MaxResponseTokens { get; set; }
    }
}
