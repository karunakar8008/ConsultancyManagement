FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ConsultancyManagement.Api/ConsultancyManagement.Api.csproj ConsultancyManagement.Api/
COPY ConsultancyManagement.Core/ConsultancyManagement.Core.csproj ConsultancyManagement.Core/
COPY ConsultancyManagement.Infrastructure/ConsultancyManagement.Infrastructure.csproj ConsultancyManagement.Infrastructure/
RUN dotnet restore ConsultancyManagement.Api/ConsultancyManagement.Api.csproj

COPY . .
RUN dotnet publish ConsultancyManagement.Api/ConsultancyManagement.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8000
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8000

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ConsultancyManagement.Api.dll"]
