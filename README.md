# chatgpt-web-dotnet-api
- 适配chatgpt-web的.net6的api，fork仓储地址 [chatgpt-web](//github.com/jkhcc11/chatgpt-web)
- 仅实现了`api`,另一种`accessToken ` 暂时未实现
- 暂时默认数据存储方式使用`LiteDb`
- 演示站点 [Gpt-666 AiChat](//ai1.gpt-666.com)

# 配置简单说明（appsettings.json）
- `ChatGptWebConfig:OpenAiBaseHost` 自定义Api反代地址，如果部署再国内需要此地址，海外直接为空即可
- `ChatGptWebConfig:ApiKeys` 支持多Api Key 轮询，视情况使用，建议只配置一个
- `ChatGptWebConfig:EveryDayFreeTimes` 每天免费共享次数，默认为`100`次
- `ChatGptWebConfig:ApiTimeoutMilliseconds` 请求OpenAi超时时间，单位（毫秒）。默认为 `30000` 
- `GeneralCodeKey` 生成和导出卡密的密钥，部署时单独设置
- `ConnectionStrings` 整个节点为`LiteDb` 链接字符串，部署时请修改连接密码

# 新增
- [x] 卡密功能（未对接卡密平台，直接生成卡密和导出）
- [x] 多Key随机轮询（不建议使用，防止封号，视情况使用）

# 待处理
- [ ] 使用`Actions`发布docker 镜像
- [ ] 新增访问量统计（百度统计|51la）
- [ ] 调整卡密检测逻辑
- [ ] 后台管理
