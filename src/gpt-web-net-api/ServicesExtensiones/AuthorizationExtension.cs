using System.Security.Claims;

namespace GptWeb.DotNet.Api.ServicesExtensiones
{
    public static class AuthorizationExtension
    {
        /// <summary>
        /// 获取用户Id
        /// </summary>
        /// <remarks>
        ///  获取授权成功后的用户Id(CardNo)
        /// </remarks>
        /// <returns></returns>
        public static string GetUserId(this ClaimsPrincipal user)
        {
            return user.Claims.First(a => a.Type == ClaimTypes.NameIdentifier).Value;
        }
    }
}
