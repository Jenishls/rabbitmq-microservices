# 1. Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the specific csproj to restore dependencies
COPY ["RabbitConsumer.csproj", "./"]
RUN dotnet restore "RabbitConsumer.csproj"

# Copy everything else and build the release app
COPY . .
RUN dotnet publish "RabbitConsumer.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Runtime Stage (Uses a tiny runtime image without the heavy SDK)
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RabbitConsumer.dll"]
    