# 1. Build Stage
FROM ://microsoft.com AS build
WORKDIR /src

# Copy only the specific csproj to restore dependencies
COPY ["RabbitConsumer1.csproj", "./"]
RUN dotnet restore "RabbitConsumer1.csproj"

# Copy everything else and build the release app
COPY . .
RUN dotnet publish "RabbitConsumer1.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Runtime Stage (Uses a tiny runtime image without the heavy SDK)
FROM ://microsoft.com AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RabbitConsumer1.dll"]
