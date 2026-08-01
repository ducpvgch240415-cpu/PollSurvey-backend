FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY src/PollSurvey.Contracts/PollSurvey.Contracts.csproj src/PollSurvey.Contracts/
COPY src/VotingService/VotingService.csproj src/VotingService/
RUN dotnet restore src/VotingService/VotingService.csproj
COPY . .
RUN dotnet publish src/VotingService/VotingService.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "VotingService.dll"]
