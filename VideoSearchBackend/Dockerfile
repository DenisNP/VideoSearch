# build server
FROM mcr.microsoft.com/dotnet/sdk:8.0 as BUILD_SERVER
WORKDIR /app
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o out

# copy server
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=BUILD_SERVER /app/out .

# run
EXPOSE 80
ENTRYPOINT ["dotnet", "VideoSearch.dll"]