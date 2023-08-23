using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using ChatGpt.Web.BaseInterface.Extensions;
using ChatGpt.Web.IService.ActivationCodeSys;
using Microsoft.AspNetCore.Http;
using ChatGpt.Web.BaseInterface;
using ChatGpt.Web.Dto;

namespace GptWeb.DotNet.Api.ServicesExtensiones
{
    /// <summary>
    /// 卡密授权
    /// </summary>
    public class ActivationCodeAuthorizationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IConfiguration _configuration;

        public ActivationCodeAuthorizationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock,
            IConfiguration configuration)
            : base(options, logger, encoder, clock)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// 检查是否有效
        /// </summary>
        /// <returns></returns>
        private async Task<bool> IsValidAuthorizationAsync(string activationCode)
        {
            var service = Request.HttpContext.RequestServices.GetService<IActivationCodeService>();
            if (service == null)
            {
                return false;
            }

            var check = await service.CheckCardNoIsValidAsync(activationCode);
            if (check.IsSuccess == false)
            {
                Logger.LogWarning("{activationCode}卡密校验401,msg:{msg}", activationCode, check.Msg);
            }

            return check.IsSuccess;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var endPoint = Context.GetEndpoint();
            if (endPoint == null)
            {
                return AuthenticateResult.Fail("error");
            }

            if (Request.Method == "OPTIONS")
            {
                return AuthenticateResult.NoResult();
            }

            //匿名标识
            var allowAnonymous = endPoint.Metadata.Any(em => em.GetType() == typeof(AllowAnonymousAttribute));
            if (allowAnonymous)
            {
                // 如果存在 [AllowAnonymous] 属性，则跳过身份验证
                return AuthenticateResult.NoResult();
            }

            // 获取请求头中的 Authorization
            var token = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(token))
            {
                Response.Headers.Add("Access-Control-Allow-Origin", "*");
                return AuthenticateResult.Fail("Token is empty");
            }

            var activationCode = token.Remove(0, "Bearer ".Length);
            var isValid = await IsValidAuthorizationAsync(activationCode);
            if (isValid == false)
            {
                // 如果令牌无效，则返回未授权状态
                Response.Headers.Add("Access-Control-Allow-Origin", "*");
                return AuthenticateResult.Fail("Invalid token");
            }

            // 创建身份验证票证 todo:这个根据角色来 暂时只有normal和admin
            CommonExtension.CommonRoleName role = CommonExtension.CommonRoleName.Normal;
            if (activationCode == _configuration.GetValue<string>("RootCardNo"))
            {
                role = CommonExtension.CommonRoleName.Root;
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.Role,role.ToString()),
                new(ClaimTypes.NameIdentifier,activationCode),
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            // 返回成功的身份验证结果
            return AuthenticateResult.Success(ticket);
        }

        protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            Response.StatusCode = StatusCodes.Status403Forbidden;
            var result = new BaseGptWebDto<string>("Forbidden")
            {
                ResultCode = KdyResultCode.Forbidden
            };
            return Response.WriteAsJsonAsync(result);
        }
    }
}
