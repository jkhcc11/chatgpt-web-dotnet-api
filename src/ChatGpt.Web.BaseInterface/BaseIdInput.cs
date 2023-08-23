using System.Collections.Generic;

namespace ChatGpt.Web.BaseInterface
{
    /// <summary>
    /// 单Id 操作Input
    /// </summary>
    public class BaseIdInput
    {
        public long Id { get; set; }
    }

    /// <summary>
    /// 多Id 操作Input
    /// </summary>
    public class BaseIdsInput
    {
        public List<long> Ids { get; set; } = new List<long>();
    }
}
