FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Finbridge.slnx ./
COPY Finbridge.Domain/Finbridge.Domain.csproj Finbridge.Domain/
COPY Finbridge.Application/Finbridge.Application.csproj Finbridge.Application/
COPY Finbridge.Data/Finbridge.Data.csproj Finbridge.Data/
COPY Finbridge.Backoffice/Finbridge.Backoffice.csproj Finbridge.Backoffice/

RUN dotnet restore Finbridge.Backoffice/Finbridge.Backoffice.csproj

COPY Finbridge.Domain/ Finbridge.Domain/
COPY Finbridge.Application/ Finbridge.Application/
COPY Finbridge.Data/ Finbridge.Data/
COPY Finbridge.Backoffice/ Finbridge.Backoffice/

RUN dotnet publish Finbridge.Backoffice/Finbridge.Backoffice.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends wget \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "Finbridge.Backoffice.dll"]
