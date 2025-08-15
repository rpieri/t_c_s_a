FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER root
WORKDIR /app
RUN apt-get update && \
    apt-get install -y curl && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/* 
USER $APP_UID  

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Carrefour.CaseFlow.Consolidado.API/", "src/Carrefour.CaseFlow.Consolidado.API/"]
COPY ["src/Carrefour.CaseFlow.Consolidado.Service/", "src/Carrefour.CaseFlow.Consolidado.Service/"]
COPY ["src/Carrefour.CaseFlow.Consolidado.Domain/", "src/Carrefour.CaseFlow.Consolidado.Domain/"]
COPY ["src/Carrefour.CaseFlow.Shared.Events/", "src/Carrefour.CaseFlow.Shared.Events/"]
COPY ["src/Carrefour.CaseFlow.Shared.Kafka/", "src/Carrefour.CaseFlow.Shared.Kafka/"]

RUN dotnet restore "src/Carrefour.CaseFlow.Consolidado.API/Carrefour.CaseFlow.Consolidado.API.csproj"
RUN dotnet build "src/Carrefour.CaseFlow.Consolidado.API/Carrefour.CaseFlow.Consolidado.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "src/Carrefour.CaseFlow.Consolidado.API/Carrefour.CaseFlow.Consolidado.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080/tcp
ENTRYPOINT ["dotnet", "Carrefour.CaseFlow.Consolidado.API.dll"]
