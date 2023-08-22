using ChatGpt.Web.BaseInterface;
using Newtonsoft.Json;

namespace ChatGpt.Web.Dto
{
    /// <summary>
    /// GptWeb
    /// </summary>
    public class BaseGptWebDto<TData>
    {
        public BaseGptWebDto(string? message)
        {
            Message = message;
        }

        /// <summary>
        /// 消息
        /// </summary>
        [JsonProperty("message")]
        public string? Message { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public KdyResultCode ResultCode { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        [JsonProperty("status")]
        public string Status => ResultCode == KdyResultCode.Success
            ? "Success"
            : ResultCode == KdyResultCode.Unauthorized ? "Unauthorized" : "Fail";

        /// <summary>
        /// 数据
        /// </summary>
        [JsonProperty("data")]
        public TData Data { get; set; } = default!;

        public bool IsSuccess => ResultCode == KdyResultCode.Success;
    }
}
