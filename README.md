# aspnet-core-3-basic-authentication-api

ASP.NET Core 3.1 - Basic HTTP Authentication API

For documentation and instructions check out https://jasonwatmore.com/post/2019/10/21/aspnet-core-3-basic-authentication-tutorial-with-example-api

to setup the identity "infrastructure"

1. First command is to install the EF Core migration tools.
dotnet tool install --global dotnet-ef --version 3.1.0

2. The Second command can be used to create a migration.
dotnet-ef migrations add First --project WebApi.csproj

3. The third one to create the database in SQL Server. The database will be created in the SQL server instance which is specified in the connection string inside appsettings.json.
dotnet-ef database update --project WebApi.csproj

