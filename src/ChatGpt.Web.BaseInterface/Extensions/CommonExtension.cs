using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ChatGpt.Web.BaseInterface.Extensions
{
    public static class CommonExtension
    {
        public const string AuthenticationScheme = "Bearer";
        /// <summary>
        /// 角色
        /// </summary>
        public enum CommonRoleName
        {
            Normal = 1,
            Root = 5
        }

        /// <summary>
        /// 随机
        /// </summary>
        /// <returns></returns>
        public static T RandomList<T>(this List<T> list)
        {
            if (list.Count == 1)
            {
                return list.First();
            }

            return list
                .OrderBy(_ => Guid.NewGuid())
                .First();
        }

        /// <summary>
        /// Obj To JsonStr
        /// </summary>
        /// <returns></returns>
        public static string ToJsonStr(this object temp)
        {
            return JsonConvert.SerializeObject(temp,
                new JsonSerializerSettings()
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
        }

        /// <summary>
        /// Obj To JsonStr
        /// </summary>
        /// <returns></returns>
        public static T StrToModel<T>(this string temp)
        {
            return JsonConvert.DeserializeObject<T>(temp);
        }
    }
}
