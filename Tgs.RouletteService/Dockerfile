#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG BuildConfiguration=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.RouletteService/Tgs.RouletteService.csproj", "Tgs.RouletteService/"]
COPY ["RouletteSignIn.Core/RouletteSignIn.Core.csproj", "RouletteSignIn.Core/"]
COPY ["RouletteSignInRepositories/RouletteSignInRepositories.csproj", "RouletteSignInRepositories/"]
RUN dotnet restore "Tgs.RouletteService/Tgs.RouletteService.csproj"
COPY . .
WORKDIR "/src/Tgs.RouletteService"
RUN dotnet build "Tgs.RouletteService.csproj" -c $BuildConfiguration -o /app/build

FROM build AS publish
ARG BuildConfiguration
RUN dotnet publish "Tgs.RouletteService.csproj" -c $BuildConfiguration -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.RouletteService.dll"]
