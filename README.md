# chatgpt-web-dotnet-api
- 适配chatgpt-web的.net6的api，fork仓储地址 [chatgpt-web](//github.com/jkhcc11/chatgpt-web)
- 仅实现了`api`,另一种`accessToken ` 暂时未实现
- 数据存储方式：`LiteDb`、`MongoDB` 使用配置文件中`SupportDbType`切换
- 演示站点 [Gpt-666 AiChat](//ai1.gpt-666.com)

# 配置说明（appsettings.json）
- `ChatGptWebConfig:ApiKeys` 支持多Api Key 轮询，视情况使用，建议只配置一个
	- `OpenAiBaseHost`: 自定义Api反代地址，如果部署再国内需要此地址，海外直接为空即可
	- `ApiKey`: ApiKey
	- `OrgId`: 组织Id  如果账号只有一个组织可不填
	- `ModelGroupName`： 模型组名，支持根据模型组分开请求apikey
        - `CustomApiHost`: 自定义ApiHost,例如转发地址等。 默认使用`sk-` 区分站点卡密和自定义卡密。
- `ChatGptWebConfig:StopFlag` 流式停止标识，默认为`[DONE]`次
- `ChatGptWebConfig:ApiTimeoutMilliseconds` 请求OpenAi超时时间，单位（毫秒）。默认为 `30000` 
- `RootCardNo` 管理员卡密,用于管理页面使用
- `SupportDbType` 数据库类型`LiteDB|MongoDB`
- `ConnectionStrings` 整个节点为DB连接字符串，部署时请修改连接密码
	- `LiteDb` LiteDb连接字符串
	- `LiteDbLog` LiteDb日志连接字符串（历史记录太大了分开）
	- `Mongodb` Mongodb连接字符串
	- `MongodbDatabaseName` Mongodb数据库名

# 新增
- [x] 卡密功能（未对接卡密平台，直接生成卡密和导出）
- [x] 多Key随机轮询（不建议使用，防止封号，视情况使用）
- [x] 新增访问量统计（百度统计|51la）
- [x] 支持根据模型请求不同的api key，比如gpt3用key1,gpt4用key2
- [x] 新增`Mongodb`存储上下文
- [x] 调整卡密检测逻辑
- [x] 后台管理
- [x] 使用`Actions`发布docker 镜像

# Docker部署

- 查看 `docker-compose.yml` docker-compose文件
或使用下面的内容(注意：挂载文件时，现在当前目录创建一个appsettings.Production.json 文件)

```

version: '3.4'

services:
  gpt-web-net-api:
     image: zcypublic/gpt-web-net-api:latest
     container_name: gpt-web-net-api
     restart: always
     environment:
      URLS: http://*:8899 #监听端口 可更改
     ports:
        - 7788:8899 #跟上面的端口对应
     volumes:
        - ./appsettings.Production.json:/app/appsettings.Production.json

```


# 配置文件例子，管理页直接输入`/admin`(你先使用cardNO授权登录，否则会提示未授权)

- 启用本地数据库 `LiteDB`

```

{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ChatGptWebConfig": {
    "ApiKeys": [
      {
        "OpenAiBaseHost": "https://xxxxx6.com",
        "ApiKey": "sk-xxxxx",
        "ModelGroupName": "gpt3"
      },
      {
        "OpenAiBaseHost": "https://ai-api-proxyproxy.xxxxx",
        "ApiKey": "sk-zjR1jGUSbE3iTQJ0OJxxxxx",
        "ModelGroupName": "gpt3_16"
      },
      {
        "ApiKey": "sk-zjR1jGUSbE3iTQJ0OJxxxxx",
        //"OrgId": "org-" //可不填，如果只有一个组织时
        "OpenAiBaseHost": "https://ai-api-proxy.xxxxx", //api反代地址
        "ModelGroupName": "gpt4"
      }
    ], //api key 支持轮询
    "CustomApiHost": "https://xxxxxxx" //自定义中转地址
  },
  "SupportDbType": "LiteDB", //
  "ConnectionStrings": {
    "LiteDb": "Filename=gtp-web-netcore-v1.db;Password=123456789;Connection=shared",
    "LiteDbLog": "Filename=gtp-web-netcore-log.db;Password=123456789;Connection=shared",
  },
  "RootCardNo": "xxxxxxxx" //管理员权限卡密 
}


```

- 启用MongoDB数据库 `MongoDB`

```

{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ChatGptWebConfig": {
    "ApiKeys": [
      {
        "OpenAiBaseHost": "https://xxxxx6.com",
        "ApiKey": "sk-xxxxx",
        "ModelGroupName": "gpt3"
      },
      {
        "OpenAiBaseHost": "https://ai-api-proxyproxy.xxxxx",
        "ApiKey": "sk-zjR1jGUSbE3iTQJ0OJxxxxx",
        "ModelGroupName": "gpt3_16"
      },
      {
        "ApiKey": "sk-zjR1jGUSbE3iTQJ0OJxxxxx",
        //"OrgId": "org-" //可不填，如果只有一个组织时
        "OpenAiBaseHost": "https://ai-api-proxy.xxxxx", //api反代地址
        "ModelGroupName": "gpt4"
      }
    ], //api key 支持轮询
    "CustomApiHost": "https://xxxxxxx" //自定义中转地址
  },
  "SupportDbType": "MongoDB",
  "ConnectionStrings": {
    "Mongodb": "mongodb://localhost:27017",
    "MongodbDatabaseName": "GptWeb"
  },
  "RootCardNo": "xxxxxxxx" //管理员权限卡密 
}


```

