using Newtonsoft.Json;

namespace ChatGpt.Web.BaseInterface
{
    /// <summary>
    /// Service 统一返回
    /// </summary>
    public class KdyResult<T> : KdyResult
    {
        public KdyResult(T data)
        {
            Data = data;
        }

        /// <summary>
        ///  数据
        /// </summary>
        public T Data { get; set; }
    }

    /// <summary>
    ///  返回格式处理
    /// </summary>
    public class KdyResult
    {
        /// <summary>
        /// 错误状态码
        /// </summary>
        public KdyResultCode Code { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? Msg { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess => Code == KdyResultCode.Success;

        /// <summary>
        /// 格式化错误数据
        /// </summary>
        /// <param name="code">错误码</param>
        /// <param name="msg">错误信息</param>
        /// <returns></returns>
        public static KdyResult Error(KdyResultCode code, string msg)
        {
            var result = new KdyResult()
            {
                Code = code,
                Msg = msg
            };
            return result;
        }

        /// <summary>
        /// 格式化错误数据
        /// </summary>
        /// <typeparam name="T">数据泛型</typeparam>
        /// <param name="code">错误码</param>
        /// <param name="msg">错误信息</param>
        /// <returns></returns>
        public static KdyResult<T> Error<T>(KdyResultCode code, string msg)
        {
            var result = new KdyResult<T>(default!)
            {
                Code = code,
                Msg = msg
            };
            return result;
        }

        /// <summary>
        /// 格式化错误数据
        /// </summary>
        /// <typeparam name="T">数据泛型</typeparam>
        /// <param name="code">错误码</param>
        /// <param name="msg">错误信息</param>
        /// <param name="date">实体</param>
        /// <returns></returns>
        public static KdyResult<T> Error<T>(KdyResultCode code, string msg, T date)
        {
            var result = new KdyResult<T>(default!)
            {
                Code = code,
                Msg = msg,
                Data = date
            };
            return result;
        }

        /// <summary>
        /// 格式化数据
        /// </summary>
        /// <typeparam name="T">数据泛型</typeparam>
        /// <param name="date">实体</param>
        /// <param name="msg">提示语</param>
        /// <returns></returns>
        public static KdyResult<T> Success<T>(T date, string msg = "")
        {
            var result = new KdyResult<T>(default!)
            {
                Code = KdyResultCode.Success,
                Msg = msg,
                Data = date
            };
            return result;
        }

        /// <summary>
        /// 操作成功
        /// </summary>
        /// <returns></returns>
        public static KdyResult Success(string msg = "操作成功")
        {
            var result = new KdyResult()
            {
                Code = KdyResultCode.Success,
                Msg = msg
            };
            return result;
        }
    }

    /// <summary>
    /// 统一状态码
    /// </summary>
    public enum KdyResultCode
    {
        /// <summary>
        /// 通用http请求错误
        /// </summary>
        HttpError = 2000,

        /// <summary>
        /// 成功
        /// </summary>
        Success = 1000,

        /// <summary>
        ///  通用错误
        /// </summary>
        Error = 900,

        /// <summary>
        ///  参数错误
        /// </summary>
        ParError = 800,

        /// <summary>
        /// 重复插入
        /// </summary>
        Duplicate = 700,

        /// <summary>
        ///  系统错误
        /// </summary>
        SystemError = 500,

        /// <summary>
        /// 未授权
        /// </summary>
        Unauthorized = 401,
        
        /// <summary>
        /// 无权访问
        /// </summary>
        Forbidden = 403,
    }
}
