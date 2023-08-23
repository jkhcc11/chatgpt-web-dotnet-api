using System.Collections.Generic;
using System.Linq;
using ChatGpt.Web.BaseInterface;
using ChatGpt.Web.Entity.Enums;

namespace ChatGpt.Web.Entity.ActivationCodeSys
{
    /// <summary>
    /// 卡密类型
    /// </summary>
    public class ActivationCodeTypeV2 : BaseEntity<long>
    {
        public const string RootCodeName = "system";
        public const string Gpt3GroupName = "gpt3";
        /// <summary>
        /// Gpt3 16k
        /// </summary>
        public const string Gpt316GroupName = "gpt3_16";
        public const string Gpt4GroupName = "gpt4";
        /// <summary>
        /// Gpt4 32k
        /// </summary>
        public const string Gpt432GroupName = "gpt4_32";

        public const string DefaultModelId = "gpt-3.5-turbo";

        protected ActivationCodeTypeV2()
        {

        }

        /// <summary>
        /// 卡密类型
        /// </summary>
        public ActivationCodeTypeV2(long id, string codeName,
            List<SupportModeItem> supportModelItems)
            : base(id)
        {
            SupportChatSystemType = SupportChatSystemType.OpenAi;
            CodeName = codeName;
            SupportModelItems = supportModelItems;
        }

        /// <summary>
        /// 卡密名称
        /// </summary>
        public string CodeName { get; set; }

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
        public List<MaxCountItem>? MaxCountItems { get; set; }

        /// <summary>
        /// 支持模型列表
        /// </summary>
        public List<SupportModeItem> SupportModelItems { get; set; }

        /// <summary>
        /// 获取最大请求次数
        /// </summary>
        /// <returns></returns>
        public List<MaxCountItem> GetMaxCountItems()
        {
            if (MaxCountItems != null &&
                MaxCountItems.Any())
            {
                return MaxCountItems;
            }

            //没设置 默认
            return new List<MaxCountItem>()
            {
                new MaxCountItem(Gpt3GroupName, 9999)
                {
                    MaxRequestToken = 3000,
                    MaxResponseToken = 1000
                },
                new MaxCountItem(Gpt4GroupName, 66)
                {
                    MaxRequestToken = 500,
                    MaxResponseToken = 500
                }
            };
        }
    }

    /// <summary>
    /// 支持模型Item
    /// </summary>
    public class SupportModeItem
    {
        public SupportModeItem(string modeId, string modeGroupName)
        {
            ModeId = modeId;
            ModeGroupName = modeGroupName;
        }

        /// <summary>
        /// 模型Id
        /// </summary>
        /// <remarks>
        /// 全模型名称 eg: gpt-3.5-turbo|gpt-4
        /// </remarks>
        public string ModeId { get; set; }

        /// <summary>
        /// 模型组名
        /// </summary>
        /// <remarks>
        /// 组名 eg: gpt3|gpt4 按组控制次数
        /// </remarks>
        public string ModeGroupName { get; set; }
    }

    /// <summary>
    /// 最大次数Item
    /// </summary>
    /// <remarks>
    /// 根据模型限制
    /// </remarks>
    public class MaxCountItem
    {
        public MaxCountItem(string modeGroupName,
            int maxCount)
        {
            ModeGroupName = modeGroupName;
            MaxCount = maxCount;
        }

        /// <summary>
        /// 模型组名
        /// </summary>
        public string ModeGroupName { get; set; }

        /// <summary>
        /// 最大次数
        /// </summary>
        public int MaxCount { get; set; }

        /// <summary>
        /// 当前请求最大Token
        /// </summary>
        /// <remarks>
        /// 为空不限制
        /// </remarks>
        public int? MaxRequestToken { get; set; }

        /// <summary>
        /// 当前返回最大Token
        /// </summary>
        /// <remarks>
        ///  为空不限制，按官方来
        /// </remarks>
        public int? MaxResponseToken { get; set; }

        /// <summary>
        /// 最大携带历史记录
        /// </summary>
        /// <remarks>
        /// 为空不限制，否则按最新10条
        /// </remarks>
        public int? MaxHistoryCount { get; set; }
    }
}
