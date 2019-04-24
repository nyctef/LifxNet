FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build
WORKDIR /app

# restore dependencies as a separate layer, since this will get cached more often
COPY src/lifxctl/*.csproj ./lifxctl/
COPY src/LifxNet/*.csproj ./LifxNet/
WORKDIR /app/lifxctl
RUN dotnet restore

# copy everything else and build
WORKDIR /app/
COPY src/lifxctl/. ./lifxctl/
COPY src/LifxNet/. ./LifxNet/
WORKDIR /app/lifxctl
RUN dotnet publish -c Release -o out


# test application -- see: dotnet-docker-unit-testing.md
#FROM build AS testrunner
#WORKDIR /app/tests
#COPY tests/. .
#ENTRYPOINT ["dotnet", "test", "--logger:trx"]


FROM mcr.microsoft.com/dotnet/core/runtime:2.2 AS runtime
WORKDIR /app
COPY --from=build /app/lifxctl/out ./
ENTRYPOINT ["dotnet", "lifxctl.dll"]