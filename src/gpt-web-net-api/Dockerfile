#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/gpt-web-net-api/gpt-web-net-api.csproj", "src/gpt-web-net-api/"]
COPY ["src/ChatGpt.Web.NetCore/ChatGpt.Web.NetCore.csproj", "src/ChatGpt.Web.NetCore/"]
RUN dotnet restore "src/gpt-web-net-api/gpt-web-net-api.csproj"
COPY . .
WORKDIR "/src/src/gpt-web-net-api"
RUN dotnet build "gpt-web-net-api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "gpt-web-net-api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "gpt-web-net-api.dll"]