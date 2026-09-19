FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["LinhaELaco.Api.csproj", "./"]
RUN dotnet restore "LinhaELaco.Api.csproj"
COPY . .
RUN dotnet publish "LinhaELaco.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "dotnet LinhaELaco.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
