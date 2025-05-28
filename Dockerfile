FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copiar arquivos de projeto e restaurar dependências
# Copia csproj e sln primeiro para reaprovitar o chache do Docker
COPY *.sln .
COPY LojaManoel/*.csproj ./LojaManoel/
RUN dotnet restore "./LojaManoel/LojaManoel.csproj"

# Copiar o restante do código da aplicação
COPY . .
WORKDIR "/source/LojaManoel"
RUN dotnet publish "./LojaManoel.csproj" -c Release -o /app/publish --no-restore

# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expor a porta que a aplicação irá rodar (verificar launchSettings.json ou Program.cs)
# O docker-compose irá mapear essa porta para a porta 80 do host
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Comando para rodar a aplicação
ENTRYPOINT ["dotnet", "LojaManoel.dll"]