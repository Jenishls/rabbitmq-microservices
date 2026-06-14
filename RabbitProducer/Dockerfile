# 1. Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["RabbitProducer.csproj", "./"]
RUN dotnet restore "RabbitProducer.csproj"
COPY . .
RUN dotnet publish "RabbitProducer.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Runtime Stage
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RabbitProducer.dll"]
 