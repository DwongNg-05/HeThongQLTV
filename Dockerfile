FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY NuGet.Config ./
COPY HeThongQLTV/HeThongQLTV.csproj HeThongQLTV/
RUN dotnet restore HeThongQLTV/HeThongQLTV.csproj
COPY HeThongQLTV/ HeThongQLTV/
RUN dotnet publish HeThongQLTV/HeThongQLTV.csproj -c Release -o /out --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out .
RUN mkdir -p /app/database /app/keys && chown -R app:app /app/database /app/keys
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    DataProtection__KeysPath=/app/keys \
    TZ=Asia/Ho_Chi_Minh
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "HeThongQLTV.dll"]
