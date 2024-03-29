﻿using System.ComponentModel.DataAnnotations;

namespace ChatGpt.Web.Dto.Inputs
{
    /// <summary>
    /// 生成卡密Input
    /// </summary>
    public class BatchGeneralCodeInput
    {
        /// <summary>
        /// 类型Id
        /// </summary>
        public long CodeTypeId { get; set; }

        /// <summary>
        /// 生成数量
        /// </summary>
        [Range(1, 500, ErrorMessage = "最大500")]
        public int Number { get; set; } = 10;
    }
}
