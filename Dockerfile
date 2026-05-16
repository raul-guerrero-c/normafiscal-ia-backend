FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["NormaFiscalIA.API/NormaFiscalIA.API.csproj", "NormaFiscalIA.API/"]
COPY ["NormaFiscalIA.Core/NormaFiscalIA.Core.csproj", "NormaFiscalIA.Core/"]
COPY ["NormaFiscalIA.Services/NormaFiscalIA.Services.csproj", "NormaFiscalIA.Services/"]

RUN dotnet restore "NormaFiscalIA.API/NormaFiscalIA.API.csproj"

COPY . .
WORKDIR "/src/NormaFiscalIA.API"
RUN dotnet build "NormaFiscalIA.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NormaFiscalIA.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "NormaFiscalIA.API.dll"]