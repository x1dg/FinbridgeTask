FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Finbridge.slnx ./
COPY Finbridge.Core/Finbridge.Core.csproj Finbridge.Core/
COPY Finbridge.Data/Finbridge.Data.csproj Finbridge.Data/
COPY Finbridge.Api/Finbridge.Api.csproj Finbridge.Api/
COPY Finbridge.Web/Finbridge.Web.csproj Finbridge.Web/
COPY Finbridge.Tests/Finbridge.Tests.csproj Finbridge.Tests/

RUN dotnet restore Finbridge.Api/Finbridge.Api.csproj

COPY Finbridge.Core/ Finbridge.Core/
COPY Finbridge.Data/ Finbridge.Data/
COPY Finbridge.Api/ Finbridge.Api/

RUN dotnet publish Finbridge.Api/Finbridge.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "Finbridge.Api.dll"]
