using System.Text;
using ChatGpt.Web.BaseInterface;
using ChatGpt.Web.BaseInterface.Extensions;
using ChatGpt.Web.BaseInterface.Options;
using ChatGpt.Web.Dto;
using ChatGpt.Web.Dto.Inputs;
using ChatGpt.Web.Dto.Request;
using ChatGpt.Web.Dto.Response;
using ChatGpt.Web.Entity.ActivationCodeSys;
using ChatGpt.Web.Entity.Enums;
using ChatGpt.Web.Entity.MessageHistory;
using ChatGpt.Web.IRepository.ActivationCodeSys;
using ChatGpt.Web.IRepository.MessageHistory;
using ChatGpt.Web.IService.OpenAiApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GptWeb.DotNet.Api.Controllers
{
    /// <summary>
    /// ChatGpt-web api����
    /// </summary>
    [ApiController]
    [Route("gpt-web-api")]
    public class GptWebController : ControllerBase
    {
        private readonly IOpenAiHttpApi _openAiHttpApi;
        private readonly IGptWebMessageRepository _webMessageRepository;
        private readonly IActivationCodeRepository _activationCodeRepository;
        private readonly IPerUseActivationCodeRecordRepository _perUseActivationCodeRecordRepository;
        private readonly IdGenerateExtension _idGenerateExtension;
        private readonly IMemoryCache _memoryCache;
        private readonly ChatGptWebConfig _chatGptWebConfig;
        private readonly ILogger<GptWebController> _logger;

        public GptWebController(IOpenAiHttpApi openAiHttpApi, IGptWebMessageRepository webMessageRepository,
            ILogger<GptWebController> logger, IdGenerateExtension idGenerateExtension,
            IActivationCodeRepository activationCodeRepository, IMemoryCache memoryCache,
            IOptions<ChatGptWebConfig> options,
            IPerUseActivationCodeRecordRepository perUseActivationCodeRecordRepository)
        {
            _openAiHttpApi = openAiHttpApi;
            _webMessageRepository = webMessageRepository;
            _logger = logger;
            _idGenerateExtension = idGenerateExtension;
            _activationCodeRepository = activationCodeRepository;
            _memoryCache = memoryCache;
            _perUseActivationCodeRecordRepository = perUseActivationCodeRecordRepository;
            _chatGptWebConfig = options.Value;
        }

        /// <summary>
        /// ����Ƿ�����֤
        /// </summary>
        /// <returns></returns>
        [HttpPost("session")]
        public async Task<IActionResult> Session()
        {
            //auth �Ƿ���Ҫ��֤
            var data = new
            {
                auth = true,
                model = "ChatGPTAPI",
            };
            var result = new BaseGptWebDto<object>()
            {
                Data = data,
                ResultCode = KdyResultCode.Success
            };
            await Task.CompletedTask;
            return new JsonResult(result);
        }

        /// <summary>
        /// �����֤
        /// </summary>
        /// <returns></returns>
        [HttpPost("verify")]
        public async Task<IActionResult> VerifyAsync(VerifyInput input)
        {
            var check = await CheckCardNoAsync(input.Token);
            if (check.IsSuccess == false)
            {
                return new JsonResult(check);
            }

            await Task.CompletedTask;
            var result = new BaseGptWebDto<string>()
            {
                Data = "login success",
                ResultCode = KdyResultCode.Success
            };
            return new JsonResult(result);
        }

        /// <summary>
        /// ����
        /// </summary>
        /// <returns></returns>
        [HttpPost("config")]
        public async Task<IActionResult> GetConfigAsync()
        {
            //���Token
            var check = await CheckRequestTokenAsync();
            if (check.IsSuccess == false)
            {
                return new JsonResult(new BaseGptWebDto<object>()
                {
                    Message = check.Message,
                    ResultCode = KdyResultCode.Error
                });
            }

            var cardNo = GetCurrentAuthCardNo();
            var cardInfo = await GetCardInfoByCacheAsync(cardNo);
            if (cardInfo.ActivateTime.HasValue == false)
            {
                return new JsonResult(new BaseGptWebDto<object>()
                {
                    Message = "��ȡʧ��",
                    ResultCode = KdyResultCode.Error
                });
            }

            var result = new BaseGptWebDto<object>()
            {
                Data = new
                {
                    expiryTime = cardInfo.ActivateTime.Value.AddDays(cardInfo.CodeType.GetHashCode())
                        .ToString("yyyy-MM-dd HH:mm:ss"),
                    activedTime = cardInfo.ActivateTime.Value
                        .ToString("yyyy-MM-dd HH:mm:ss")
                },
                ResultCode = KdyResultCode.Success
            };
            return new JsonResult(result);
        }

        /// <summary>
        /// ��ʽ������������
        /// </summary>
        /// <returns></returns>
        [HttpPost("chat-process")]
        public async Task ChatProcessAsync(ChatProcessInput input)
        {
            Response.ContentType = "application/octet-stream";
            var writer = new StreamWriter(Response.Body);

            //���Token
            var check = await CheckRequestTokenAsync();
            if (check.IsSuccess == false)
            {
                await writer.WriteLineAsync(Newtonsoft.Json.JsonConvert.SerializeObject(check));
                await writer.FlushAsync();
                return;
            }

            var token = GetCurrentAuthCardNo();
            var key = _chatGptWebConfig.ApiKeys.RandomList();
            var isFirst = string.IsNullOrEmpty(input.Options.ParentMessageId);
            var reqMessages = new List<SendChatCompletionsMessageItem>()
            {
                new("system",input.SystemMessage),
            };

            GptWebMessage? userMessage = null;
            if (isFirst == false)
            {
                //���״� ��ȡ������
                reqMessages = await BuildMsgContextAsync(input.Options.ParentMessageId);
            }

            reqMessages.Add(new("user", input.Prompt));
            var request = new SendChatCompletionsRequest(reqMessages)
            {
                Stream = true
            };

            if (isFirst == false)
            {
                var assistantMessage = await _webMessageRepository.GetMessageByParentMsgIdAsync(input.Options.ParentMessageId);
                //todo:�ظ���ʧʱ δ����
                userMessage = await SaveUserMessage(assistantMessage, input.Prompt,
                    Newtonsoft.Json.JsonConvert.SerializeObject(request));
            }

            var result = await _openAiHttpApi.SendChatCompletionsAsync(key, request);
            if (result.IsSuccess == false)
            {
                _logger.LogError("Gpt����ʧ�ܣ�{reqMsg},{responseMsg}",
                    Newtonsoft.Json.JsonConvert.SerializeObject(request),
                    Newtonsoft.Json.JsonConvert.SerializeObject(result));
                await writer.WriteLineAsync(result.Msg);
                await writer.FlushAsync();
                return;
            }

            if (result.Data.ResponseStream == null)
            {
                _logger.LogError("��ȡ��Ӧ��ʧЧ��{reqMsg},{responseMsg}",
                    Newtonsoft.Json.JsonConvert.SerializeObject(request),
                    Newtonsoft.Json.JsonConvert.SerializeObject(result));
                await writer.WriteLineAsync("no");
                await writer.FlushAsync();
                return;
            }

            await using (result.Data.ResponseStream)
            {
                using var reader = new StreamReader(result.Data.ResponseStream);
                var prefixLength = "data: ".Length;
                var roleStr = "";
                var assistantId = "";
                var response = "";
                var currentAnswer = new StringBuilder();
                while (!reader.EndOfStream)
                {
                    #region ��ʽ����
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(line) ||
                        line.Contains("[DONE]"))
                    {
                        //��������Ϊ�ղ���
                        continue;
                    }

                    var jsonStr = line.Remove(0, prefixLength);
                    var tempData = Newtonsoft.Json.JsonConvert.DeserializeObject<SendChatCompletionsResponse>(jsonStr);
                    if (string.IsNullOrEmpty(assistantId) &&
                        string.IsNullOrEmpty(tempData?.ChatId) == false)
                    {
                        //ֻ��Ҫ��ȡһ�μ���
                        assistantId = tempData.ChatId;
                    }

                    var deltaObj = tempData?.Choices.FirstOrDefault()?.DeltaObj;
                    var currentDelta = "";
                    var roleToken = deltaObj?["role"];
                    if (roleToken != null)
                    {
                        roleStr = roleToken + "";
                    }

                    var contentToken = deltaObj?["content"];
                    if (contentToken != null)
                    {
                        currentDelta = contentToken + "";
                        currentAnswer.Append(currentDelta);
                    }

                    var newObj = new
                    {
                        role = roleStr,
                        id = tempData?.ChatId,
                        parentMessageId = "", //
                        delta = currentDelta,
                        text = currentAnswer.ToString()
                    };

                    await writer.WriteLineAsync(Newtonsoft.Json.JsonConvert.SerializeObject(newObj));
                    await writer.FlushAsync();
                    #endregion
                }

                if (isFirst)
                {
                    await CreateFirstMsgAsync(input.Prompt, currentAnswer.ToString()
                        , assistantId
                        , Newtonsoft.Json.JsonConvert.SerializeObject(request)
                        , response
                        , input.SystemMessage);
                }
                else
                {
                    if (userMessage == null)
                    {
                        _logger.LogWarning("�ǵ�һ������,�û���Ϣ��ʧ��user:{msg}", input.Prompt);
                        return;
                    }

                    await SaveAssistantContentMessage(userMessage, assistantId, currentAnswer.ToString(),
                        response);

                }

                //��¼
                await _perUseActivationCodeRecordRepository.CreateAsync(new PerUseActivationCodeRecord(
                    _idGenerateExtension.GenerateId(),
                    token));
            }

        }

        #region ˽��
        /// <summary>
        /// �����û��״���Ϣ������ParentId��
        /// </summary>
        /// <param name="userMsg">�û���Ϣ</param>
        /// <param name="assistantContent">�ش�����</param>
        /// <param name="assistantId">�ش�Id</param>
        /// <param name="request">����json</param>
        /// <param name="response">����json todo:��ʱ����¼����ԭʼ����
        /// </param>
        /// <param name="systemMsg">ϵͳ��Ϣ������У�</param>
        /// <returns></returns>
        private async Task CreateFirstMsgAsync(string userMsg, string assistantContent, string assistantId,
            string request, string response, string systemMsg = "")
        {
            var messages = new List<GptWebMessage>();
            var conversationId = _idGenerateExtension.GenerateId();

            if (string.IsNullOrEmpty(systemMsg) == false)
            {
                //ϵͳ��Ϣ
                var systemMessage = new GptWebMessage(_idGenerateExtension.GenerateId(), systemMsg,
                    MsgType.System, conversationId);
                messages.Add(systemMessage);
            }

            //�û���Ϣ
            var userMessage = new GptWebMessage(_idGenerateExtension.GenerateId(), userMsg,
                MsgType.User, conversationId)
            {
                GtpRequest = request
            };
            messages.Add(userMessage);

            //�ظ���Ϣ
            var assistantMessage = new GptWebMessage(_idGenerateExtension.GenerateId(), assistantContent,
                MsgType.Assistant, conversationId)
            {
                ParentId = userMessage.ParentId,
                GtpResponse = response,
                GptMsgId = assistantId
            };
            messages.Add(assistantMessage);

            await _webMessageRepository.BatchCreateAsync(messages);
        }

        /// <summary>
        /// ����Gpt��ϢId������Ϣ������
        /// </summary>
        /// <returns></returns>
        private async Task<List<SendChatCompletionsMessageItem>> BuildMsgContextAsync(string gptMsgId)
        {
            var result = new List<SendChatCompletionsMessageItem>();
            var assistantMessage = await _webMessageRepository.GetMessageByParentMsgIdAsync(gptMsgId);
            if (assistantMessage == null)
            {
                return result;
            }


            //��ǰ�Ự��������Ϣ
            var currentConversationMessage =
                await _webMessageRepository.QueryMsgByConversationIdAsync(assistantMessage.ConversationId);
            foreach (var message in currentConversationMessage.OrderBy(a => a.CreatedTime))
            {
                result.Add(new SendChatCompletionsMessageItem(message.MsgType.ToString().ToLower(),
                    message.Msg));
            }

            return result;
        }

        /// <summary>
        /// �����û���Ϣ
        /// </summary>
        /// <param name="parentMsg">����Ϣ</param>
        /// <param name="userMsg">�û���Ϣ</param>
        /// <param name="request">������Ϣ</param>
        /// <returns></returns>
        private async Task<GptWebMessage> SaveUserMessage(GptWebMessage parentMsg, string userMsg, string request)
        {
            //�û���Ϣ
            var userMessage = new GptWebMessage(_idGenerateExtension.GenerateId(), userMsg,
                MsgType.User, parentMsg.ConversationId)
            {
                GtpRequest = request,
                ParentId = parentMsg.Id,
                GptMsgId = parentMsg.GptMsgId
            };

            await _webMessageRepository.CreateAsync(userMessage);
            return userMessage;
        }

        /// <summary>
        /// ���淵����Ϣ
        /// </summary>
        /// <returns></returns>
        private async Task SaveAssistantContentMessage(GptWebMessage userMessage,
            string assistantId, string assistantContent, string response)
        {
            //�ظ���Ϣ
            var assistantMessage = new GptWebMessage(_idGenerateExtension.GenerateId(), assistantContent,
                MsgType.Assistant, userMessage.ConversationId)
            {
                ParentId = userMessage.Id,
                GtpResponse = response,
                GptMsgId = assistantId
            };

            await _webMessageRepository.CreateAsync(assistantMessage);
        }

        /// <summary>
        /// ���CardNo
        /// </summary>
        /// <returns></returns>
        private async Task<BaseGptWebDto<object>> CheckCardNoAsync(string cardNo)
        {
            var cacheValue = await GetCardInfoByCacheAsync(cardNo);
            if (cacheValue.CodeType == default)
            {
                return new BaseGptWebDto<object>()
                {
                    ResultCode = KdyResultCode.Error,
                    Message = "��Ч����,��ȷ������"
                };
            }

            if (cacheValue.ActivateTime.HasValue == false)
            {
                cacheValue.ActivateTime = DateTime.Now;
                await _activationCodeRepository.UpdateAsync(cacheValue);
            }

            var expiryTime = cacheValue.ActivateTime.Value.AddDays(cacheValue.CodeType.GetHashCode());
            if (DateTime.Now > expiryTime)
            {
                return new BaseGptWebDto<object>()
                {
                    ResultCode = KdyResultCode.Error,
                    Message = "�����ѹ���,�����ѻ����¹���"
                };
            }

            //���μ���
            if (cacheValue.CodeType == ActivationCodeType.PerUse)
            {
                var count = await _perUseActivationCodeRecordRepository.CountTimesAsync(DateTime.Today, cardNo);
                if (count > _chatGptWebConfig.EveryDayFreeTimes)
                {
                    return new BaseGptWebDto<object>()
                    {
                        ResultCode = KdyResultCode.Error,
                        Message = $"������Ѷ��,�����ꡣ��Ѵ�����{_chatGptWebConfig.EveryDayFreeTimes}"
                    };
                }
            }

            return new BaseGptWebDto<object>()
            {
                ResultCode = KdyResultCode.Success
            };
        }

        /// <summary>
        /// �������Token
        /// </summary>
        /// <returns></returns>
        private async Task<BaseGptWebDto<object>> CheckRequestTokenAsync()
        {
            var cardNo = GetCurrentAuthCardNo();
            if (string.IsNullOrEmpty(cardNo))
            {
                return new BaseGptWebDto<object>()
                {
                    ResultCode = KdyResultCode.Unauthorized
                };
            }

            var check = await CheckCardNoAsync(cardNo);
            if (check.IsSuccess == false)
            {
                check.ResultCode = KdyResultCode.Unauthorized;
            }

            return check;
        }

        /// <summary>
        /// ��ȡ����Ϣ����
        /// </summary>
        /// <returns></returns>
        private async Task<ActivationCode> GetCardInfoByCacheAsync(string cardNo)
        {
            var cacheKey = $"m:cardNo:{cardNo}";
            var cacheValue = _memoryCache.Get<ActivationCode>(cacheKey);
            if (cacheValue == null)
            {
                cacheValue = await _activationCodeRepository.GetActivationCodeByCardNoAsync(cardNo)
                             ?? new ActivationCode(_idGenerateExtension.GenerateId(), cardNo, default);

                if (cacheValue.ActivateTime.HasValue)
                {
                    //��ֵ����30������Ч
                    _memoryCache.Set(cacheKey, cacheValue, TimeSpan.FromMinutes(30));
                }
            }

            return cacheValue;
        }

        /// <summary>
        /// ��ȡ��ǰ��Ȩ����
        /// </summary>
        /// <returns></returns>
        private string GetCurrentAuthCardNo()
        {
            var typeIndex = "Bearer ".Length;
            var token = Request.Headers.Authorization.ToString();
            if (string.IsNullOrEmpty(token) ||
                token.Length < typeIndex)
            {
                return string.Empty;
            }

            return token.Remove(0, typeIndex);
        }
        #endregion

    }
}