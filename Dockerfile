FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore
COPY ["AcxiomCRM.Domain/AcxiomCRM.Domain.csproj", "AcxiomCRM.Domain/"]
COPY ["AcxiomCRM.Application/AcxiomCRM.Application.csproj", "AcxiomCRM.Application/"]
COPY ["AcxiomCRM.Infrastructure/AcxiomCRM.Infrastructure.csproj", "AcxiomCRM.Infrastructure/"]
COPY ["AcxiomCRM.Web/AcxiomCRM.Web.csproj", "AcxiomCRM.Web/"]
COPY ["AcxiomCRM.Tests/AcxiomCRM.Tests.csproj", "AcxiomCRM.Tests/"]
COPY ["AcxiomCRM.sln", "./"]

RUN dotnet restore "AcxiomCRM.sln"

# Copy all source files and build
COPY . .
WORKDIR "/src/AcxiomCRM.Web"
RUN dotnet publish "AcxiomCRM.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENV PORT=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "AcxiomCRM.Web.dll"]
