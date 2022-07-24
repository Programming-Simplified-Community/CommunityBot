FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["BotService/BotService.csproj", "BotService/"]
RUN dotnet restore "BotService/BotService.csproj"
COPY . .
WORKDIR "/src/BotService"
RUN dotnet build "BotService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BotService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BotService.dll"]
