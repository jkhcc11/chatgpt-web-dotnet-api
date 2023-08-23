using ChatGpt.Web.BaseInterface;

namespace ChatGpt.Web.Dto.Inputs.ActivationCodeAdmin
{
    /// <summary>
    /// 分页获取卡密类型
    /// </summary>
    public class QueryPageCodeTypeInput : QueryPageInput
    {
        /// <summary>
        /// 关键字
        /// </summary>
        public string? KeyWord { get; set; }
    }
}
