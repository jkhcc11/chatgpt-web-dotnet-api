using System;
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;
using ChatGpt.Web.BaseInterface;
using ChatGpt.Web.Dto;
using ChatGpt.Web.Entity.ActivationCodeSys;
using ChatGpt.Web.IRepository.ActivationCodeSys;
using ChatGpt.Web.IService.ActivationCodeSys;
using Microsoft.Extensions.Caching.Memory;

namespace ChatGpt.Web.NetCore.ActivationCodeSys
{
    /// <summary>
    /// 卡密 服务实现
    /// </summary>
    public class ActivationCodeService : IActivationCodeService
    {
        private readonly IActivationCodeTypeV2Repository _activationCodeTypeV2Repository;
        private readonly IActivationCodeRepository _activationCodeRepository;
        private readonly IPerUseActivationCodeRecordRepository _perUseActivationCodeRecordRepository;
        private readonly IMemoryCache _memoryCache;

        public ActivationCodeService(IActivationCodeRepository activationCodeRepository,
            IMemoryCache memoryCache, IActivationCodeTypeV2Repository activationCodeTypeV2Repository,
            IPerUseActivationCodeRecordRepository perUseActivationCodeRecordRepository)
        {
            _activationCodeRepository = activationCodeRepository;
            _memoryCache = memoryCache;
            _activationCodeTypeV2Repository = activationCodeTypeV2Repository;
            _perUseActivationCodeRecordRepository = perUseActivationCodeRecordRepository;
        }

        /// <summary>
        /// 根据卡密获取卡密缓存
        /// </summary>
        /// <returns></returns>
        public async Task<ActivationCode?> GetCardInfoByCacheAsync(string cardNo)
        {
            var cacheKey = $"m:cardNo:{cardNo}";
            var cacheValue = await _memoryCache.GetOrCreateAsync(cacheKey, async (cacheEntry) =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                var cacheValue = await _activationCodeRepository.GetActivationCodeByCardNoAsync(cardNo);
                if (cacheValue == null)
                {
                    return null;
                }

                if (cacheValue.ActivateTime.HasValue)
                {
                    //有值卡密30分钟生效
                    cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
                }

                return cacheValue;
            });

            return cacheValue;
        }

        /// <summary>
        /// 获取卡类型缓存
        /// </summary>
        /// <returns></returns>
        public async Task<ActivationCodeTypeV2> GetCodeTypeByCacheAsync(long codeTypeId)
        {
            var cacheKey = $"m:codyType:{codeTypeId}";
            var cacheValue = await _memoryCache.GetOrCreateAsync(cacheKey, async (cacheEntry) =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60);
                return await _activationCodeTypeV2Repository.GetEntityByIdAsync(codeTypeId); ;
            });

            return cacheValue;
        }

        /// <summary>
        /// 检查卡密是否有效
        /// </summary>
        /// <returns></returns>
        public async Task<KdyResult> CheckCardNoIsValidAsync(string cardNo)
        {
            var cardInfoCache = await GetCardInfoByCacheAsync(cardNo);
            if (cardInfoCache == null)
            {
                return KdyResult.Error(KdyResultCode.Unauthorized, "无效卡密");
            }

            if (cardInfoCache.ActivateTime.HasValue == false)
            {
                //第一次授权时 不走统一授权 走单独的设置激活时间
                return KdyResult.Error(KdyResultCode.Error, "卡密异常");
            }

            //卡类型
            var codeType = await GetCodeTypeByCacheAsync(cardInfoCache.CodyTypeId);
            var expiryTime = cardInfoCache.ActivateTime.Value.AddDays(codeType.ValidDays);
            if (DateTime.Now > expiryTime)
            {
                return KdyResult.Error(KdyResultCode.Unauthorized, "卡密已过期");
            }

            return KdyResult.Success();
        }

        /// <summary>
        /// 首次检查卡密是否有效
        /// </summary>
        /// <remarks>
        ///  首次验卡校验时,需要设置激活时间
        /// </remarks>
        /// <returns></returns>
        public async Task<KdyResult> CheckCardNoIsValidWithFirstAsync(string cardNo)
        {
            var cardInfoCache = await GetCardInfoByCacheAsync(cardNo);
            if (cardInfoCache == null)
            {
                return KdyResult.Error(KdyResultCode.Unauthorized, "无效卡密");
            }

            if (cardInfoCache.ActivateTime.HasValue == false)
            {
                cardInfoCache.ActivateTime = DateTime.Now;
                await _activationCodeRepository.UpdateAsync(cardInfoCache);
            }

            //卡类型
            var codeType = await GetCodeTypeByCacheAsync(cardInfoCache.CodyTypeId);
            var expiryTime = cardInfoCache.ActivateTime.Value.AddDays(codeType.ValidDays);
            if (DateTime.Now > expiryTime)
            {
                return KdyResult.Error(KdyResultCode.Unauthorized, "卡密已过期");
            }

            return KdyResult.Success();
        }

        /// <summary>
        /// 检查卡密是否有访问权限
        /// </summary>
        /// <param name="cardInfo">卡信息</param>
        /// <param name="codeType">卡密类型</param>
        /// <param name="modelId">模型Id</param>
        /// <returns></returns>
        public async Task<KdyResult> CheckCardNoIsAccessAsync(ActivationCode cardInfo, ActivationCodeTypeV2 codeType, string modelId)
        {
            await Task.CompletedTask;
            //模型分组信息
            var supportModelItem = codeType.SupportModelItems.FirstOrDefault(a => a.ModeId == modelId);
            if (supportModelItem == null)
            {
                return KdyResult.Error(KdyResultCode.Forbidden, $"当前卡密不支持【{modelId}】,请切换模型或更换卡密");
            }

            return KdyResult.Success();
        }

        /// <summary>
        /// 检查卡密当天请求次数
        /// </summary>
        /// <param name="cardInfo">卡信息</param>
        /// <param name="codeType">卡密类型</param>
        /// <param name="supportModeItem">模型Item</param>
        /// <returns></returns>
        public async Task<KdyResult> CheckTodayCardNoTimesAsync(ActivationCode cardInfo, ActivationCodeTypeV2 codeType,
            SupportModeItem supportModeItem)
        {
            if (codeType.IsEveryDayResetCount == false)
            {
                //非每天计算次数卡密
                return KdyResult.Error(KdyResultCode.Error, "检测异常,非此类检测类型");
            }

            //当前模型组最大配置
            var modelMax = codeType.GetMaxCountItems()
                .FirstOrDefault(a => a.ModeGroupName == supportModeItem.ModeGroupName);
            if (modelMax == null)
            {
                //未配置不限制
                return KdyResult.Success();
            }

            //按次计费
            var count = await _perUseActivationCodeRecordRepository
                .CountTimesByGroupNameAsync(cardInfo.CardNo, supportModeItem.ModeGroupName, DateTime.Today);
            if (count > modelMax.MaxCount)
            {
                return KdyResult.Error(KdyResultCode.Forbidden, $"模型：{supportModeItem.ModeGroupName},今天额度已耗尽。请更换卡密。\r\n卡密今日最大次数：{modelMax.MaxCount}");
                //return new BaseGptWebDto<object>()
                //{
                //    ResultCode = KdyResultCode.Error,
                //    Message = $"模型：{supportModelItem.ModeGroupName},今天额度已耗尽。请更换卡密。\r\n卡密今日最大次数：{modelMax.MaxCount}"
                //};
            }

            return KdyResult.Success();

        }

        /// <summary>
        /// 检查卡密所有访问次数
        /// </summary>
        /// <param name="cardInfo">卡信息</param>
        /// <param name="codeType">卡密类型</param>
        /// <param name="supportModeItem">模型Item</param>
        /// <returns></returns>
        public async Task<KdyResult> CheckCardNoTimesAsync(ActivationCode cardInfo, ActivationCodeTypeV2 codeType,
            SupportModeItem supportModeItem)
        {
            if (codeType.IsEveryDayResetCount)
            {
                return KdyResult.Error(KdyResultCode.Error, "检测异常,非此类检测类型");
            }

            //当前模型组最大配置
            var modelMax = codeType.GetMaxCountItems()
                .FirstOrDefault(a => a.ModeGroupName == supportModeItem.ModeGroupName);
            if (modelMax == null)
            {
                return KdyResult.Success();
            }

            //按次计费
            var count = await _perUseActivationCodeRecordRepository
                .CountTimesByGroupNameAsync(cardInfo.CardNo, supportModeItem.ModeGroupName, null);
            if (count > modelMax.MaxCount)
            {
                return KdyResult.Error(KdyResultCode.Forbidden, $"模型：{supportModeItem.ModeGroupName},额度已耗尽。请更换卡密。\r\n卡密最大次数：{modelMax.MaxCount}");
                //return new BaseGptWebDto<object>()
                //{
                //    ResultCode = KdyResultCode.Error,
                //    Message = $"模型：{supportModelItem.ModeGroupName},额度已耗尽。请更换卡密。\r\n卡密最大次数：{modelMax.MaxCount}"
                //};
            }

            return KdyResult.Success();
        }
    }
}
