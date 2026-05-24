FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["FinBot.App/FinBot.App.csproj", "FinBot.App/"]
COPY ["FinBot.Bll.Implementation/FinBot.Bll.Implementation.csproj", "FinBot.Bll.Implementation/"]
COPY ["FinBot.Bll.Interfaces/FinBot.Bll.Interfaces.csproj", "FinBot.Bll.Interfaces/"]
COPY ["FinBot.Domain/FinBot.Domain.csproj", "FinBot.Domain/"]
COPY ["FinBot.Dal/FinBot.Dal.csproj", "FinBot.Dal/"]
COPY ["FinBot.Integrations/FinBot.Integrations.csproj", "FinBot.Integrations/"]
RUN dotnet restore "FinBot.App/FinBot.App.csproj"
COPY . .
WORKDIR "/src/FinBot.App"
RUN dotnet build "./FinBot.App.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./FinBot.App.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FinBot.App.dll"]
