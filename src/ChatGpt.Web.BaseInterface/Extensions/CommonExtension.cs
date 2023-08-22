using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

        /// <summary>
        /// Query转Lambda表达式
        /// </summary>
        /// <returns></returns>
        public static Expression<Func<TEntity, bool>>? ToExpressionPredicate<TEntity>(this IQueryable<TEntity> query)
        {
            Expression<Func<TEntity, bool>>? predicate = null;

            if (query.Expression is MethodCallExpression methodCallExpression
                && methodCallExpression.Method.Name == "Where")
            {
                var arguments = methodCallExpression.Arguments;
                var lambdaExpressions = new List<LambdaExpression>();

                foreach (var argument in arguments)
                {
                    if (argument is UnaryExpression unaryExpression
                        && unaryExpression.Operand is LambdaExpression lambdaExpression)
                    {
                        lambdaExpressions.Add(lambdaExpression);
                    }
                }

                if (lambdaExpressions.Count > 0)
                {
                    predicate = (Expression<Func<TEntity, bool>>)lambdaExpressions.Aggregate((expr1, expr2) =>
                        Expression.Lambda<Func<TEntity, bool>>(
                            Expression.AndAlso(expr1.Body, expr2.Body),
                            expr1.Parameters[0]
                        )
                    );
                }
            }

            return predicate;

            //Expression<Func<TEntity, bool>>? predicate = null;

            //if (query.Expression is MethodCallExpression methodCallExpression
            //    && methodCallExpression.Method.Name == "Where"
            //    && methodCallExpression.Arguments[1] is UnaryExpression unaryExpression
            //    && unaryExpression.Operand is LambdaExpression lambdaExpression)
            //{
            //    predicate = (Expression<Func<TEntity, bool>>)lambdaExpression;
            //}

            //return predicate;
        }
    }
}
