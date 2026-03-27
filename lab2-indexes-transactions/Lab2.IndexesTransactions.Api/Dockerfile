FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Lab2.IndexesTransactions.Api/Lab2.IndexesTransactions.Api.csproj", "Lab2.IndexesTransactions.Api/"]
RUN dotnet restore "./Lab2.IndexesTransactions.Api/Lab2.IndexesTransactions.Api.csproj"
COPY . .
WORKDIR "/src/Lab2.IndexesTransactions.Api"
RUN dotnet build "./Lab2.IndexesTransactions.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Lab2.IndexesTransactions.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Lab2.IndexesTransactions.Api.dll"]