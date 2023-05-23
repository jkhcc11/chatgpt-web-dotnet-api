using System.Threading.Tasks;
using ChatGpt.Web.BaseInterface;
using ChatGpt.Web.Dto.Request;
using ChatGpt.Web.Dto.Response;

namespace ChatGpt.Web.IService.OpenAiApi
{
    /// <summary>
    /// OpenAi控制台 接口
    /// </summary>
    public interface IOpenAiDashboardApiHttpApi
    {
        /// <summary>
        /// 获取账号账单
        /// </summary>
        /// <returns></returns>
        Task<KdyResult<GetAccountBillingResponse>> GetAccountBillingAsync(GetAccountBillingRequest request);
    }
}
