using System;

namespace ChatGpt.Web.BaseInterface
{
    /// <summary>
    /// 基础Entity
    /// </summary>
    public abstract class BaseEntity<TKey>
    {
        protected BaseEntity(TKey id)
        {
            Id = id;
        }

        /// <summary>
        /// 主键
        /// </summary>
        public TKey Id { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// 用户Id
        /// </summary>
        public long? CreatedUserId { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime? ModifyTime { get; set; }

        /// <summary>
        /// 修改人
        /// </summary>
        public long? ModifyUserId { get; set; }
    }
}
