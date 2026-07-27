FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

COPY FinancesApi.sln .
COPY src ./src

RUN dotnet restore src/FinancesApp_Api/FinancesApp_Api.csproj

RUN dotnet publish src/FinancesApp_Api/FinancesApp_Api.csproj -c $BUILD_CONFIGURATION -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

EXPOSE 8080

COPY --from=base /app/publish .

ENTRYPOINT ["dotnet", "FinancesApp_Api.dll"]