using System.Collections.Generic;
using ChatGpt.Web.Entity.ActivationCodeSys;

namespace ChatGpt.Web.Entity.Enums
{
    /// <summary>
    /// 卡密类型
    /// </summary>
    public enum ActivationCodeType
    {
        /// <summary>
        /// 天
        /// </summary>
        OneDay = 1,

        /// <summary>
        /// 4.0一天
        /// </summary>
        OneDay4 = 2,

        /// <summary>
        /// 周
        /// </summary>
        Weekly = 7,

        /// <summary>
        /// 4.0 7天
        /// </summary>
        Weekly4 = 8,

        /// <summary>
        /// 月
        /// </summary>
        Month = 30,

        /// <summary>
        /// 4.0 30天
        /// </summary>
        Month4 = 31,

        /// <summary>
        /// 年
        /// </summary>
        Year = 366,

        /// <summary>
        /// 4.0 年
        /// </summary>
        Year4 = 367,

        /// <summary>
        /// Gpt4按次
        /// </summary>
        PerUse4 = 998,

        /// <summary>
        /// 按次
        /// </summary>
        PerUse = 999,

        /// <summary>
        /// 超Vip
        /// </summary>
        SuperVip = 9999
    }

    /// <summary>
    /// 扩展
    /// </summary>
    public static class ActivationCodeTypeExtension
    {
        /// <summary>
        /// 根据类型返回对应有效天数
        /// </summary>
        /// <returns></returns>
        public static int GetValidDaysByCodeType(this ActivationCodeType codeType)
        {
            switch (codeType)
            {
                case ActivationCodeType.OneDay4:
                case ActivationCodeType.OneDay:
                    {
                        return 1;
                    }
                case ActivationCodeType.Weekly4:
                case ActivationCodeType.Weekly:
                    {
                        return 7;
                    }
                case ActivationCodeType.Month4:
                case ActivationCodeType.Month:
                    {
                        return 30;
                    }
                case ActivationCodeType.Year:
                case ActivationCodeType.Year4:
                    {
                        return 366;
                    }
                default:
                    {
                        return codeType.GetHashCode();
                    }
            }
        }

        /// <summary>
        /// 模型分组转模型列表
        /// </summary>
        /// <returns></returns>
        public static List<SupportModeItem> GetSupportModeItemsByGroupName(this string groupName)
        {
            //todo:这里后面groupName和modelId 可配置
            var supportModelItems = new List<SupportModeItem>();
            switch (groupName)
            {
                case ActivationCodeTypeV2.Gpt432GroupName:
                    {
                        supportModelItems.Add(new SupportModeItem("gpt-4-32k", groupName));
                        supportModelItems.Add(new SupportModeItem("gpt-4-32k-0613", groupName));
                        break;
                    }
                case ActivationCodeTypeV2.Gpt4GroupName:
                    {
                        supportModelItems.Add(new SupportModeItem("gpt-4", groupName));
                        supportModelItems.Add(new SupportModeItem("gpt-4-0613", groupName));
                        supportModelItems.Add(new SupportModeItem("gpt-4o", groupName));
                        supportModelItems.Add(new SupportModeItem("gpt-4o-mini", groupName));
                        supportModelItems.Add(new SupportModeItem("gpt-4-vision-preview", groupName));
                        break;
                    }
                case ActivationCodeTypeV2.Gpt316GroupName:
                    {
                        supportModelItems.Add(new SupportModeItem("gpt-3.5-turbo-16k", groupName));
                        supportModelItems.Add(new SupportModeItem("gpt-3.5-turbo-16k-0613", groupName));
                        break;
                    }
                default:
                    {
                        supportModelItems.Add(new SupportModeItem("gpt-3.5-turbo", groupName));
                        supportModelItems.Add(new SupportModeItem("gpt-3.5-turbo-0613", groupName));
                        break;
                    }
            }

            return supportModelItems;
        }
    }
}
