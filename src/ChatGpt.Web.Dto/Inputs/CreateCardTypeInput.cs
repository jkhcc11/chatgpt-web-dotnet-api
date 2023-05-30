using System.Collections.Generic;

namespace ChatGpt.Web.Dto.Inputs
{
    /// <summary>
    /// 创建卡类型
    /// </summary>
    public class CreateCardTypeInput
    {
        /// <summary>
        /// 密钥
        /// </summary>
        public string GeneralCodeKey { get; set; }

        public CreateCardTypeInput(string cardTypeName,
            List<string> supportModelGroupNameItems,
            int validDays, string generalCodeKey)
        {
            CardTypeName = cardTypeName;
            SupportModelGroupNameItems = supportModelGroupNameItems;
            ValidDays = validDays;
            GeneralCodeKey = generalCodeKey;
        }

        /// <summary>
        /// 卡类型名
        /// </summary>
        public string CardTypeName { get; set; }

        /// <summary>
        /// 支持模型组  gpt3|gpt4
        /// </summary>
        public List<string> SupportModelGroupNameItems { get; set; }

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
        public List<LimitItem>? LimitItems { get; set; }
    }

    /// <summary>
    /// 限制Item
    /// </summary>
    public class LimitItem
    {
        public LimitItem(string supportModelGroupName)
        {
            SupportModelGroupName = supportModelGroupName;
        }

        /// <summary>
        /// 模型组名
        /// </summary>
        public string SupportModelGroupName { get; set; }

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
