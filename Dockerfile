FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY PartnerIntegration.Bff.sln ./
COPY src/PartnerIntegration.Domain/PartnerIntegration.Domain.csproj src/PartnerIntegration.Domain/
COPY src/PartnerIntegration.Application/PartnerIntegration.Application.csproj src/PartnerIntegration.Application/
COPY src/PartnerIntegration.Infrastructure/PartnerIntegration.Infrastructure.csproj src/PartnerIntegration.Infrastructure/
COPY src/PartnerIntegration.Api/PartnerIntegration.Api.csproj src/PartnerIntegration.Api/
COPY tests/PartnerIntegration.UnitTests/PartnerIntegration.UnitTests.csproj tests/PartnerIntegration.UnitTests/

RUN dotnet restore PartnerIntegration.Bff.sln

COPY . .
RUN dotnet publish src/PartnerIntegration.Api/PartnerIntegration.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development
EXPOSE 8080
ENTRYPOINT ["dotnet", "PartnerIntegration.Api.dll"]
