FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/Common/Common.csproj src/Common/
COPY src/InventoryService/InventoryService.csproj src/InventoryService/
RUN dotnet restore src/InventoryService/InventoryService.csproj
COPY src/Common/ src/Common/
COPY src/InventoryService/ src/InventoryService/
RUN dotnet publish src/InventoryService/InventoryService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "InventoryService.dll"]
