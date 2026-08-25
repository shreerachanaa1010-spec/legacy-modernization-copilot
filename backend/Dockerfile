FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/LegacyModernization.Api/LegacyModernization.Api.csproj src/LegacyModernization.Api/
COPY src/LegacyModernization.Analyzer/LegacyModernization.Analyzer.csproj src/LegacyModernization.Analyzer/
COPY src/LegacyModernization.Core/LegacyModernization.Core.csproj src/LegacyModernization.Core/
COPY src/LegacyModernization.LLM/LegacyModernization.LLM.csproj src/LegacyModernization.LLM/
COPY src/LegacyModernization.TestGenerator/LegacyModernization.TestGenerator.csproj src/LegacyModernization.TestGenerator/
COPY src/LegacyModernization.Verifier/LegacyModernization.Verifier.csproj src/LegacyModernization.Verifier/
RUN dotnet restore src/LegacyModernization.Api/LegacyModernization.Api.csproj

COPY . .
RUN dotnet publish src/LegacyModernization.Api/LegacyModernization.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "LegacyModernization.Api.dll"]
