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
    }
}
