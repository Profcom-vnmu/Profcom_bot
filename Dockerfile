FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копіюємо файли проекту
COPY ["StudentUnionBot.csproj", "./"]
RUN dotnet restore "StudentUnionBot.csproj"

# Копіюємо всі файли та будуємо
COPY . .
RUN dotnet build "StudentUnionBot.csproj" -c Release -o /app/build
RUN dotnet publish "StudentUnionBot.csproj" -c Release -o /app/publish

# Фінальний образ
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Створюємо папку для бази даних
RUN mkdir -p Data

# Файли admins.txt та ban.txt вже скопійовані разом з publish
# Якщо їх немає - створюємо порожні
RUN touch admins.txt ban.txt || true

ENTRYPOINT ["dotnet", "StudentUnionBot.dll"]
