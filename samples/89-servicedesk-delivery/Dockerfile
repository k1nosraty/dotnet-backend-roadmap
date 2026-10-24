FROM mcr.microsoft.com/dotnet/sdk:10.0.402 AS build
WORKDIR /src
COPY SupportDesk.csproj ./
RUN dotnet restore SupportDesk.csproj
COPY . ./
ARG COURSE_COMMIT=local
RUN dotnet publish SupportDesk.csproj -c Release --no-restore -o /out -p:UseAppHost=false -p:CourseCommit=$COURSE_COMMIT
FROM mcr.microsoft.com/dotnet/aspnet:10.0.13
WORKDIR /app
COPY --from=build /out ./
RUN mkdir -p /var/lib/servicedesk/keys && chown $APP_UID:$APP_UID /var/lib/servicedesk/keys && chmod 700 /var/lib/servicedesk/keys
ENV ASPNETCORE_HTTP_PORTS=8080
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet","SupportDesk.Lesson90.dll"]
