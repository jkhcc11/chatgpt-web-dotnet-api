using ChatGpt.Web.BaseInterface;

namespace ChatGpt.Web.Dto.Inputs
{
    /// <summary>
    /// 分页获取站点配置
    /// </summary>
    public class QueryPageWebConfigInput : QueryPageInput
    {
        /// <summary>
        /// 关键字
        /// </summary>
        public string? KeyWord { get; set; }
    }
}
