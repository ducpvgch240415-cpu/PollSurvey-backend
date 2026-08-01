FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY src/ApiGateway/ApiGateway.csproj src/ApiGateway/
RUN dotnet restore src/ApiGateway/ApiGateway.csproj
COPY . .
RUN dotnet publish src/ApiGateway/ApiGateway.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
