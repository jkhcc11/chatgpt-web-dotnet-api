using System;

namespace ChatGpt.Web.BaseInterface.Extensions
{
    public static class DateTimeExtension
    {
        /// <summary>
        /// 转秒 时间戳
        /// </summary>
        /// <returns></returns>
        public static long ToSecondTimestamp(this DateTime date)
        {
            return (date.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
        }

        /// <summary>
        /// 转毫秒 时间戳
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static long ToMillisecondTimestamp(this DateTime date)
        {
            return (date.ToUniversalTime().Ticks - 621355968000000000) / 10000;
        }

        /// <summary>
        /// 时间戳转本地时间(秒)
        /// </summary>
        /// <returns></returns>
        public static DateTime ToDataTimeByTimestamp(this int timestamp)
        {
            return DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime;
        }

        /// <summary>
        /// 时间戳转本地时间(毫秒)
        /// </summary>
        /// <returns></returns>
        public static DateTime ToDataTimeByTimestamp(this long timestamp)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).LocalDateTime;
        }
    }
}
