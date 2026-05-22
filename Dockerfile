# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["iDiski.Api/iDiski.Api.csproj", "iDiski.Api/"]
COPY ["iDiski.Application/iDiski.Application.csproj", "iDiski.Application/"]
COPY ["iDiski.Domain/iDiski.Domain.csproj", "iDiski.Domain/"]
COPY ["iDiski.Infrastructure/iDiski.Infrastructure.csproj", "iDiski.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "iDiski.Api/iDiski.Api.csproj"

# Copy everything else
COPY . .

# Build and publish
WORKDIR "/src/iDiski.Api"
RUN dotnet publish "iDiski.Api.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# Create uploads directory
RUN mkdir -p wwwroot/uploads/players wwwroot/uploads/teams wwwroot/uploads/sponsors

# Expose port (Railway will set PORT env var)
EXPOSE 8080

# Start the application
# Railway will set PORT env var, ASP.NET Core will use it automatically
CMD ASPNETCORE_URLS=http://*:$PORT dotnet iDiski.Api.dll
