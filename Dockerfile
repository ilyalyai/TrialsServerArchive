# Stage 1: Сборка
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем решение и файлы проекта
COPY TrialsServerArchive.sln ./
COPY TrialsServerArchive/*.csproj ./TrialsServerArchive/

# Восстанавливаем зависимости
RUN dotnet restore

# Копируем остальные файлы
COPY TrialsServerArchive ./TrialsServerArchive

# Публикуем проект
WORKDIR /src/TrialsServerArchive
RUN dotnet publish -c Release -o /publish

# Stage 2: Запуск
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /publish ./
ENTRYPOINT ["dotnet", "TrialsServerArchive.dll"]