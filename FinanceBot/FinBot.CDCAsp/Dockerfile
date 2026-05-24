FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["FinBot.CDCAsp/FinBot.CDCAsp.csproj", "FinBot.CDCAsp/"]
COPY ["FinBot.Observability/FinBot.Observability.csproj", "FinBot.Observability/"]
COPY ["FinBot.Dal/FinBot.Dal.csproj", "FinBot.Dal/"]
COPY ["FinBot.Domain/FinBot.Domain.csproj", "FinBot.Domain/"]
RUN dotnet restore "FinBot.CDCAsp/FinBot.CDCAsp.csproj"
COPY . .
WORKDIR "/src/FinBot.CDCAsp"
RUN dotnet build "FinBot.CDCAsp.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "FinBot.CDCAsp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FinBot.CDCAsp.dll"]
