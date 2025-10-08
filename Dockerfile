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

# Копіюємо конфігураційні файли
COPY admins.txt .
COPY ban.txt .

ENTRYPOINT ["dotnet", "StudentUnionBot.dll"]
