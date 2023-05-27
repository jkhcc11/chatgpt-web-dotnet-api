using System;
using System.Collections.Generic;
using System.Text;

namespace ChatGpt.Web.Dto
{
    /// <summary>
    /// 流式返回聊天内容
    /// </summary>
    public class ChatProcessDto
    {
        /// <summary>
        /// 角色Id
        /// </summary>
        public string? Role { get; set; }

        /// <summary>
        /// 聊天Id
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// 差异|变化量（流式返回使用）
        /// </summary>
        public string? Delta { get; set; }

        /// <summary>
        /// 显示文本
        /// </summary>
        public string Text { get; set; } = "";

        /// <summary>
        /// 父节点Id
        /// </summary>
        public string? ParentMessageId { get; set; }
    }
}
