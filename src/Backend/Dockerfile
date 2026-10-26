FROM mcr.microsoft.com/dotnet/sdk:8.0 AS base
WORKDIR /app
EXPOSE 5000

FROM base AS development
RUN dotnet tool install -g dotnet-watch
ENV PATH="${PATH}:/root/.dotnet/tools"
COPY . .
WORKDIR /app/src/Backend/MonProfAI.Api
CMD ["dotnet", "watch", "run", "--no-restore"]

FROM base AS build
COPY . .
WORKDIR /app/src/Backend/MonProfAI.Api
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
ENTRYPOINT ["dotnet", "MonProfAI.Api.dll"]
