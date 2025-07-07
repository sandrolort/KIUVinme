FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["KiuVinme/KiuVinme.csproj", "KiuVinme/"]
RUN dotnet restore "KiuVinme/KiuVinme.csproj"
COPY . .
WORKDIR "/src/KiuVinme"
RUN dotnet build "KiuVinme.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "KiuVinme.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KiuVinme.dll"]