﻿using System.Collections.Generic;
using System.Threading.Tasks;
using ChatGpt.Web.Entity.ActivationCodeSys;
using ChatGpt.Web.IRepository.ActivationCodeSys;

namespace ChatGpt.Web.LiteDatabase.Repository
{
    /// <summary>
    /// 卡密信息 仓储实现
    /// </summary>
    public class ActivationCodeRepository : BaseLiteDatabaseRepository<ActivationCode, long>, IActivationCodeRepository
    {
        public ActivationCodeRepository(LiteDB.LiteDatabase liteDatabase) : base(liteDatabase)
        {
        }

        /// <summary>
        /// 卡密号查卡密
        /// </summary>
        /// <returns></returns>
        public async Task<ActivationCode?> GetActivationCodeByCardNoAsync(string cardNo)
        {
            await Task.CompletedTask;
            return DbCollection.FindOne(a => a.CardNo == cardNo);
        }

        /// <summary>
        ///  根据类型获取卡密
        /// </summary>
        /// <param name="codeTypeId">卡密类型ID</param>
        /// <returns></returns>
        public async Task<List<ActivationCode>> QueryActivationCodeByTypeAsync(long? codeTypeId)
        {
            var query = DbCollection.Query();
            if (codeTypeId.HasValue)
            {
                query = query.Where(a => a.CodyTypeId == codeTypeId.Value);
            }

            await Task.CompletedTask;
            return query.ToList();
        }
    }
}
