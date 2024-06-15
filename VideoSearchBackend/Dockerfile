# build client
FROM node:22 as BUILD_CLIENT
COPY ../video-search-frontend /app
WORKDIR /app
RUN npm install --silent
RUN npm run build

# build server
FROM mcr.microsoft.com/dotnet/sdk:8.0 as BUILD_SERVER
WORKDIR /app
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o out

# copy
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=BUILD_SERVER /app/out .
COPY --from=BUILD_CLIENT /app/dist /app/wwwroot

# run
EXPOSE 8080
ENTRYPOINT ["dotnet", "VideoSearch.dll"]