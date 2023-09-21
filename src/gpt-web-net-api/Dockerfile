#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/gpt-web-net-api/gpt-web-dotnet-api.csproj", "src/gpt-web-net-api/"]
COPY ["src/ChatGpt.Web.LiteDatabase/ChatGpt.Web.LiteDatabase.csproj", "src/ChatGpt.Web.LiteDatabase/"]
COPY ["src/ChatGpt.Web.IRepository/ChatGpt.Web.IRepository.csproj", "src/ChatGpt.Web.IRepository/"]
COPY ["src/ChatGpt.Web.Entity/ChatGpt.Web.Entity.csproj", "src/ChatGpt.Web.Entity/"]
COPY ["src/ChatGpt.Web.BaseInterface/ChatGpt.Web.BaseInterface.csproj", "src/ChatGpt.Web.BaseInterface/"]
COPY ["src/ChatGpt.Web.MongoDB/ChatGpt.Web.MongoDB.csproj", "src/ChatGpt.Web.MongoDB/"]
COPY ["src/ChatGpt.Web.NetCore/ChatGpt.Web.NetCore.csproj", "src/ChatGpt.Web.NetCore/"]
COPY ["src/ChatGpt.Web.IService/ChatGpt.Web.IService.csproj", "src/ChatGpt.Web.IService/"]
COPY ["src/ChatGpt.Web.Dto/ChatGpt.Web.Dto.csproj", "src/ChatGpt.Web.Dto/"]
RUN dotnet restore "src/gpt-web-net-api/gpt-web-dotnet-api.csproj"
COPY . .
WORKDIR "/src/src/gpt-web-net-api"
RUN dotnet build "gpt-web-dotnet-api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "gpt-web-dotnet-api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "gpt-web-dotnet-api.dll"]