using System;
using System.Collections.Generic;
using System.Linq;

namespace ChatGpt.Web.BaseInterface.Extensions
{
    public static class CommonExtension
    {
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
    }
}
