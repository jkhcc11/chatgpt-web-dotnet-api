using ChatGpt.Web.BaseInterface;

namespace ChatGpt.Web.Dto.Inputs.ActivationCodeAdmin
{
    /// <summary>
    /// 分页获取卡密
    /// </summary>
    public class QueryPageActivationCodeInput : QueryPageInput
    {
        /// <summary>
        /// 关键字
        /// </summary>
        public string? KeyWord { get; set; }
    }
}
