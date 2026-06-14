# 1. Build Stage
FROM ://microsoft.com AS build
WORKDIR /src

# Copy only the specific csproj to restore dependencies
COPY ["RabbitConsumer2.csproj", "./"]
RUN dotnet restore "RabbitConsumer2.csproj"

# Copy everything else and build the release app
COPY . .
RUN dotnet publish "RabbitConsumer2.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Runtime Stage (Uses a tiny runtime image without the heavy SDK)
FROM ://microsoft.com AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RabbitConsumer2.dll"]
