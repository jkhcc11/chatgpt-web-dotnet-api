using ChatGpt.Web.BaseInterface;
using ChatGpt.Web.BaseInterface.Extensions;
using ChatGpt.Web.BaseInterface.Options;
using ChatGpt.Web.IRepository;
using ChatGpt.Web.IRepository.ActivationCodeSys;
using ChatGpt.Web.IRepository.MessageHistory;
using ChatGpt.Web.LiteDatabase;
using LiteDB;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GptWeb.DotNet.Api.Controllers
{
    /// <summary>
    /// 数据迁移
    /// </summary>
    [ApiController]
    [Route("data-migration")]
    [Authorize(Roles = nameof(CommonExtension.CommonRoleName.Root))]
    public class DataMigrationController : BaseController
    {
        private readonly IActivationCodeRepository _activationCodeRepository;
        private readonly IActivationCodeTypeV2Repository _activationCodeTypeV2Repository;
        private readonly IGptWebConfigRepository _gptWebConfigRepository;
        private readonly IPerUseActivationCodeRecordRepository _perUseActivationCodeRecordRepository;
        private readonly IGptWebMessageRepository _gptWebMessageRepository;

        private readonly LiteDB.LiteDatabase _liteDatabase;
        private readonly LogLiteDatabase _logLiteDatabase;

        public DataMigrationController(IActivationCodeRepository activationCodeRepository,
            IConfiguration configuration, IActivationCodeTypeV2Repository activationCodeTypeV2Repository,
            IGptWebConfigRepository gptWebConfigRepository,
            IPerUseActivationCodeRecordRepository perUseActivationCodeRecordRepository,
            IGptWebMessageRepository gptWebMessageRepository)
        {
            _activationCodeRepository = activationCodeRepository;
            _activationCodeTypeV2Repository = activationCodeTypeV2Repository;
            _gptWebConfigRepository = gptWebConfigRepository;

            _perUseActivationCodeRecordRepository = perUseActivationCodeRecordRepository;
            _gptWebMessageRepository = gptWebMessageRepository;

            var connectionString = configuration.GetValue<string>("ConnectionStrings:LiteDb");
            var logConnectionString = configuration.GetValue<string>("ConnectionStrings:LiteDbLog");
            _liteDatabase = new LiteDatabase(connectionString);
            _logLiteDatabase = new LogLiteDatabase(logConnectionString);
        }

        /// <summary>
        /// 迁移卡类型
        /// </summary>
        /// <returns></returns>
        [HttpGet("card-type")]
        public async Task<IActionResult> MigrationCardAsync(string codeKey)
        {
            var oldRepository = new ChatGpt.Web.LiteDatabase.Repository.ActivationCodeTypeV2Repository(_liteDatabase);
            var oldEntities = await oldRepository.GetAllActivationCodeTypeAsync();

            await _activationCodeTypeV2Repository.CreateAsync(oldEntities);
            return Content($"数量：{oldEntities.Count}");
        }

        [HttpGet("code")]
        public async Task<IActionResult> MigrationCodeAsync(string codeKey)
        {
            var oldRepository = new ChatGpt.Web.LiteDatabase.Repository.ActivationCodeRepository(_liteDatabase);
            var oldEntities = await oldRepository.GetAllListAsync();

            await _activationCodeRepository.CreateAsync(oldEntities.ToList());
            return Content($"数量：{oldEntities.Count}");
        }

        [HttpGet("code-record")]
        public async Task<IActionResult> MigrationCodeRecordAsync(string codeKey)
        {
            var oldRepository = new ChatGpt.Web.LiteDatabase.Repository.PerUseActivationCodeRecordRepository(_liteDatabase);
            var oldEntities = await oldRepository.GetAllListAsync();

            await _perUseActivationCodeRecordRepository.CreateAsync(oldEntities.ToList());
            return Content($"数量：{oldEntities.Count}");
        }

        [HttpGet("history")]
        public async Task<IActionResult> MigrationHistoryAsync(string codeKey)
        {
            var oldRepository = new ChatGpt.Web.LiteDatabase.Repository.GptWebMessageRepository(_liteDatabase, _logLiteDatabase);
            var oldEntities = await oldRepository.GetAllListAsync();

            await _gptWebMessageRepository.CreateAsync(oldEntities.ToList());
            return Content($"数量：{oldEntities.Count}");
        }

        [HttpGet("config")]
        public async Task<IActionResult> MigrationConfigAsync(string codeKey)
        {
            var oldRepository = new ChatGpt.Web.LiteDatabase.Repository.GptWebConfigRepository(_liteDatabase);
            var oldEntities = await oldRepository.GetAllListAsync();

            await _gptWebConfigRepository.CreateAsync(oldEntities.ToList());
            return Content($"数量：{oldEntities.Count}");
        }
    }
}
