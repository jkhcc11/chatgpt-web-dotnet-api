using System.Collections.Generic;

namespace ChatGpt.Web.BaseInterface
{
    public class QueryPageDto<TDto>
    {
        /// <summary>
        /// 总数
        /// </summary>
        public long Total { get; set; }

        /// <summary>
        /// 列表
        /// </summary>
        public IReadOnlyList<TDto> Items { get; set; } = new List<TDto>();
    }

    public abstract class QueryPageInput
    {
        /// <summary>
        /// 分页大小
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// 页
        /// </summary>
        public int Page { get; set; }
    }
}
