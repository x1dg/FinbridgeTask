FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Finbridge.slnx ./
COPY Finbridge.Web/Finbridge.Web.csproj Finbridge.Web/

RUN dotnet restore Finbridge.Web/Finbridge.Web.csproj

COPY Finbridge.Web/ Finbridge.Web/

RUN dotnet publish Finbridge.Web/Finbridge.Web.csproj \
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

ENTRYPOINT ["dotnet", "Finbridge.Web.dll"]
