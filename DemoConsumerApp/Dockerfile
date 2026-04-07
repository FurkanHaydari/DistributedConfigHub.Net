FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["DemoConsumerApp/DemoConsumerApp.csproj", "DemoConsumerApp/"]
COPY ["DistributedConfigHub.Client/DistributedConfigHub.Client.csproj", "DistributedConfigHub.Client/"]

RUN dotnet restore "DemoConsumerApp/DemoConsumerApp.csproj"

COPY . .
WORKDIR "/src/DemoConsumerApp"
RUN dotnet build "DemoConsumerApp.csproj" -c Release -o /app/build
RUN dotnet publish "DemoConsumerApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DemoConsumerApp.dll"]
