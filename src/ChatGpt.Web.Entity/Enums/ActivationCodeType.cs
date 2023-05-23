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
        /// 周
        /// </summary>
        Weekly = 7,

        /// <summary>
        /// 月
        /// </summary>
        Month = 30,

        /// <summary>
        /// 年
        /// </summary>
        Year = 366,

        /// <summary>
        /// 按次
        /// </summary>
        PerUse = 999,

        /// <summary>
        /// 超Vip
        /// </summary>
        SuperVip = 9999
    }
}
