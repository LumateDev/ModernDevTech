FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копируем файлы проектов
COPY ["Lab2.IndexesTransactions.Api/Lab2.IndexesTransactions.Api.csproj", "Lab2.IndexesTransactions.Api/"]
COPY ["Lab2.IndexesTransactions.Tests/Lab2.IndexesTransactions.Tests.csproj", "Lab2.IndexesTransactions.Tests/"]

# Восстанавливаем зависимости тестового проекта (он ссылается на API)
RUN dotnet restore "Lab2.IndexesTransactions.Tests/Lab2.IndexesTransactions.Tests.csproj"

# Копируем весь исходный код
COPY . .

# Переходим в папку тестов и запускаем тесты
WORKDIR "/src/Lab2.IndexesTransactions.Tests"
ENTRYPOINT ["dotnet", "test", "Lab2.IndexesTransactions.Tests.csproj", "--no-restore", "--logger:console;verbosity=detailed"]