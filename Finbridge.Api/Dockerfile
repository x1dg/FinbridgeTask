FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Finbridge.slnx ./
COPY Finbridge.Domain/Finbridge.Domain.csproj Finbridge.Domain/
COPY Finbridge.Application/Finbridge.Application.csproj Finbridge.Application/
COPY Finbridge.Data/Finbridge.Data.csproj Finbridge.Data/
COPY Finbridge.Api/Finbridge.Api.csproj Finbridge.Api/

RUN dotnet restore Finbridge.Api/Finbridge.Api.csproj

COPY Finbridge.Domain/ Finbridge.Domain/
COPY Finbridge.Application/ Finbridge.Application/
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
