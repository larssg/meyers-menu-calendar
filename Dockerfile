# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY Meyers.Web/Meyers.Web.csproj Meyers.Web/
COPY Meyers.Test/Meyers.Test.csproj Meyers.Test/
RUN dotnet restore Meyers.Web/Meyers.Web.csproj

# Copy everything else and build
COPY . .
RUN dotnet publish Meyers.Web/Meyers.Web.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

# Configure ASP.NET Core to listen on port 8080
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Meyers.Web.dll"]