using System;

namespace ChatGpt.Web.BaseInterface
{
    public abstract class BaseEntityDto<TKey>
    where TKey : struct
    {
        /// <summary>
        /// 主键
        /// </summary>
        public virtual TKey Id { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public virtual DateTime CreatedTime { get; set; }

        /// <summary>
        /// 用户Id
        /// </summary>
        public virtual long? CreatedUserId { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        public virtual DateTime? ModifyTime { get; set; }

        /// <summary>
        /// 修改人
        /// </summary>
        public virtual long? ModifyUserId { get; set; }
    }
}
