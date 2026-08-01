FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY src/PollSurvey.Contracts/PollSurvey.Contracts.csproj src/PollSurvey.Contracts/
COPY src/RealtimeService/RealtimeService.csproj src/RealtimeService/
RUN dotnet restore src/RealtimeService/RealtimeService.csproj
COPY . .
RUN dotnet publish src/RealtimeService/RealtimeService.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "RealtimeService.dll"]
