FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Ekomart.slnx ./
COPY Ekomart.Domain/Ekomart.Domain.csproj Ekomart.Domain/
COPY Ekomart.Application/Ekomart.Application.csproj Ekomart.Application/
COPY Ekomart.Infrastructure/Ekomart.Infrastructure.csproj Ekomart.Infrastructure/
COPY Ekomart.Seeder/Ekomart.Seeder.csproj Ekomart.Seeder/
COPY Ekomart.Web/Ekomart.Web.csproj Ekomart.Web/

RUN dotnet restore Ekomart.slnx

COPY . .

RUN dotnet publish Ekomart.Web/Ekomart.Web.csproj -c Release -o /app/web /p:UseAppHost=false
RUN dotnet publish Ekomart.Seeder/Ekomart.Seeder.csproj -c Release -o /app/seeder /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS aspnet-base
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

FROM aspnet-base AS seeder
WORKDIR /app
COPY --from=build /app/seeder .
ENTRYPOINT ["dotnet", "Ekomart.Seeder.dll"]

FROM aspnet-base AS web
WORKDIR /app
COPY --from=build /app/web .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Ekomart.Web.dll"]
