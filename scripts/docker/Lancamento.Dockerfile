FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER root
WORKDIR /app

USER $APP_UID  

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Carrefour.CaseFlow.Lancamentos.API/", "src/Carrefour.CaseFlow.Lancamentos.API/"]
COPY ["src/Carrefour.CaseFlow.Lancamentos.Application/", "src/Carrefour.CaseFlow.Lancamentos.Application/"]
COPY ["src/Carrefour.CaseFlow.Lancamentos.Domain/", "src/Carrefour.CaseFlow.Lancamentos.Domain/"]
COPY ["src/Carrefour.CaseFlow.Lancamentos.Infrastructure/", "src/Carrefour.CaseFlow.Lancamentos.Infrastructure/"]
COPY ["src/Carrefour.CaseFlow.Shared.Events/", "src/Carrefour.CaseFlow.Shared.Events/"]
COPY ["src/Carrefour.CaseFlow.Shared.Kafka/", "src/Carrefour.CaseFlow.Shared.Kafka/"]

RUN dotnet restore "src/Carrefour.CaseFlow.Lancamentos.API/Carrefour.CaseFlow.Lancamentos.API.csproj"
RUN dotnet build "src/Carrefour.CaseFlow.Lancamentos.API/Carrefour.CaseFlow.Lancamentos.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "src/Carrefour.CaseFlow.Lancamentos.API/Carrefour.CaseFlow.Lancamentos.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080/tcp
ENTRYPOINT ["dotnet", "Carrefour.CaseFlow.Lancamentos.API.dll"]
