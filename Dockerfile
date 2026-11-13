FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/ReservaSalas/ReservaSalas.csproj src/ReservaSalas/
COPY src/ReservaSalas.Api/ReservaSalas.Api.csproj src/ReservaSalas.Api/
RUN dotnet restore src/ReservaSalas.Api/ReservaSalas.Api.csproj
COPY src/ src/
RUN dotnet publish src/ReservaSalas.Api/ReservaSalas.Api.csproj -c Release -o /app --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet","ReservaSalas.Api.dll"]