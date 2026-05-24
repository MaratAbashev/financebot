FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["FinBot.Llm/FinBot.Llm.csproj", "FinBot.Llm/"]
COPY ["FinBot.Integrations/FinBot.Integrations.csproj", "FinBot.Integrations/"]
COPY ["FinBot.Bll.Interfaces/FinBot.Bll.Interfaces.csproj", "FinBot.Bll.Interfaces/"]
COPY ["FinBot.Domain/FinBot.Domain.csproj", "FinBot.Domain/"]
COPY ["FinBot.Dal/FinBot.Dal.csproj", "FinBot.Dal/"]
COPY ["FinBot.Kafka/FinBot.Kafka.csproj", "FinBot.Kafka/"]
RUN dotnet restore "FinBot.Llm/FinBot.Llm.csproj"
COPY . .
WORKDIR "/src/FinBot.Llm"
RUN dotnet build "./FinBot.Llm.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./FinBot.Llm.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FinBot.Llm.dll"]
