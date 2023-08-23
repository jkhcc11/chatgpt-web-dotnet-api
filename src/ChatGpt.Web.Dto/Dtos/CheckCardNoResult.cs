using ChatGpt.Web.BaseInterface;
using ChatGpt.Web.BaseInterface.Options;
using ChatGpt.Web.Entity.ActivationCodeSys;

namespace ChatGpt.Web.Dto.Dtos
{
    public class CheckCardNoResult : KdyResult
    {
        /// <summary>
        /// 模型限制
        /// </summary>
        public MaxCountItem? MaxCountItem { get; set; }

        /// <summary>
        /// ApiKey
        /// </summary>
        public ApiKeyItem? KeyItem { get; set; }

        /// <summary>
        /// 支持模型Item
        /// </summary>
        /// <remarks>
        /// 用于限制次数
        /// </remarks>
        public SupportModeItem? SupportModeItem { get; set; }

        public static CheckCardNoResult Error(string? msg)
        {
            return new CheckCardNoResult()
            {
                Code = KdyResultCode.Error,
                Msg = msg
            };
        }

        public static CheckCardNoResult Success(MaxCountItem? maxCountItem,
            ApiKeyItem keyItem,
            SupportModeItem supportModeItem)
        {
            return new CheckCardNoResult()
            {
                Code = KdyResultCode.Success,
                MaxCountItem = maxCountItem,
                KeyItem = keyItem,
                SupportModeItem = supportModeItem
            };
        }
    }
}
