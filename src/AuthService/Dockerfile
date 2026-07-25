FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/Common/Common.csproj src/Common/
COPY src/AuthService/AuthService.csproj src/AuthService/
RUN dotnet restore src/AuthService/AuthService.csproj
COPY src/Common/ src/Common/
COPY src/AuthService/ src/AuthService/
RUN dotnet publish src/AuthService/AuthService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "AuthService.dll"]
