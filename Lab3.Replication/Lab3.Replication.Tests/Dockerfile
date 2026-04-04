FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Lab3.Replication.Node/Lab3.Replication.Node.csproj", "Lab3.Replication.Node/"]
COPY ["Lab3.Replication.Tests/Lab3.Replication.Tests.csproj", "Lab3.Replication.Tests/"]
RUN dotnet restore "Lab3.Replication.Tests/Lab3.Replication.Tests.csproj"

COPY . .

WORKDIR "/src/Lab3.Replication.Tests"
ENTRYPOINT ["dotnet", "test", "Lab3.Replication.Tests.csproj", "--no-restore", "--logger:console;verbosity=detailed"]