# 1. Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["RabbitConsumer.csproj", "./"]
RUN dotnet restore "RabbitConsumer.csproj"
COPY . .
RUN dotnet publish "RabbitConsumer.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Runtime Stage
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RabbitConsumer.dll"]
 