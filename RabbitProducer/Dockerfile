# 1. Build Stage
FROM ://microsoft.com AS build
WORKDIR /src
COPY ["RabbitProducer.csproj", "./"]
RUN dotnet restore "RabbitProducer.csproj"

COPY . .
RUN dotnet publish "RabbitProducer.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Runtime Stage (Uses ASP.NET core for web/API endpoints)
FROM ://microsoft.com AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RabbitProducer.dll"]
