FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
WORKDIR /source

COPY src/core/ERP.Domain/ERP.Domain.csproj                       src/core/ERP.Domain/
COPY src/core/ERP.Application/ERP.Application.csproj             src/core/ERP.Application/
COPY src/infrastructure/ERP.Infrastructure/ERP.Infrastructure.csproj src/infrastructure/ERP.Infrastructure/
COPY src/presentation/ERP.WebApi/ERP.WebApi.csproj               src/presentation/ERP.WebApi/
RUN dotnet restore src/presentation/ERP.WebApi/ERP.WebApi.csproj

COPY src/ src/
RUN dotnet publish src/presentation/ERP.WebApi/ERP.WebApi.csproj \
    --configuration Release \
    --no-restore \
    --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0.12 AS runtime
WORKDIR /app

COPY --from=build /app ./
RUN mkdir -p /app/Logs && chown -R $APP_UID:$APP_UID /app/Logs

USER $APP_UID
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "ERP.WebApi.dll"]
