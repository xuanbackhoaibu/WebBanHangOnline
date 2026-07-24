FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY global.json ./
COPY WebBanHangOnline/WebBanHangOnline.csproj WebBanHangOnline/
RUN dotnet restore WebBanHangOnline/WebBanHangOnline.csproj

COPY . .
RUN dotnet publish WebBanHangOnline/WebBanHangOnline.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WebBanHangOnline.dll"]
