SQLDatabase Binding for Azure Functions (v1 runtime - .NET Framework)

https://github.com/davidpallmann/sql-database-binding-azurefunc-v1

For details on installing and using, see the github readme.

This sample contains 2 projects:

1. SQLDatabaseExtension: this is the binding itself, which adds a [SqlDatabase] binding to Azure Functions.
2. FunctionApp: an app for testing/demonstrating the SQLDatabase binding.

For creating the sample Book table and data records for the sample, create an Azure SQL Database, connect to it in SSMS, and run the script CreateBooksDatabase.sql


