FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY src/PollService/PollService.csproj src/PollService/
RUN dotnet restore src/PollService/PollService.csproj
COPY . .
RUN dotnet publish src/PollService/PollService.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "PollService.dll"]
