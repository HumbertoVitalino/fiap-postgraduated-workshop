FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /app

COPY Directory.Build.props .

COPY src/Fiap.Workshop.Domain/Fiap.Workshop.Domain.csproj \
     src/Fiap.Workshop.Domain/
COPY src/Fiap.Workshop.Application/Fiap.Workshop.Application.csproj \
     src/Fiap.Workshop.Application/
COPY src/Fiap.Workshop.Infrastructure/Fiap.Workshop.Infrastructure.csproj \
     src/Fiap.Workshop.Infrastructure/
COPY src/Fiap.Workshop.Api/Fiap.Workshop.Api.csproj \
     src/Fiap.Workshop.Api/

RUN dotnet restore src/Fiap.Workshop.Api/Fiap.Workshop.Api.csproj

FROM restore AS publish
COPY src/ src/
RUN dotnet publish src/Fiap.Workshop.Api/Fiap.Workshop.Api.csproj \
    --no-restore \
    --configuration Release \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Fiap.Workshop.Api.dll"]
