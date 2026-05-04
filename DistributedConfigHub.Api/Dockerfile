FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# We only copy projects (restore) first to cache layers
COPY ["DistributedConfigHub.Api/DistributedConfigHub.Api.csproj", "DistributedConfigHub.Api/"]
COPY ["DistributedConfigHub.Application/DistributedConfigHub.Application.csproj", "DistributedConfigHub.Application/"]
COPY ["DistributedConfigHub.Domain/DistributedConfigHub.Domain.csproj", "DistributedConfigHub.Domain/"]
COPY ["DistributedConfigHub.Infrastructure/DistributedConfigHub.Infrastructure.csproj", "DistributedConfigHub.Infrastructure/"]
COPY ["DistributedConfigHub.Client/DistributedConfigHub.Client.csproj", "DistributedConfigHub.Client/"]

RUN dotnet restore "DistributedConfigHub.Api/DistributedConfigHub.Api.csproj"

# Copy the rest and build
COPY . .
WORKDIR "/src/DistributedConfigHub.Api"
RUN dotnet build "DistributedConfigHub.Api.csproj" -c Release -o /app/build
RUN dotnet publish "DistributedConfigHub.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DistributedConfigHub.Api.dll"]
