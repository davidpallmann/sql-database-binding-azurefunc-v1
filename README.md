# sql-database-binding-azurefunc-v1
Azure SQL Database binding for Azure Functions v1. (.NET Framework)

This project adds Input and Output Bindings for Azure SQL Database to Azure Functions v1 (.NET Framework). With it, you can enjoy the same ease-of-use and minimal code in Azure Functions that other binding users enjoy.

# Usage

This proejct is .NET Framework-based, and therefore is only compatible v1 Azure Functions, such as C# functions developed in Visual Studio.

Your Visual Studio solution should contain your function app project plus this project or a reference to it.

In your C# function code, add a using statemnt for the SQLDatabaseExtension namespace

```
using SQLDatabaseExtension;
```

In your function code, add a [SQLDatabase] attribute when you need an input binding or output binding to Azure SQL Database. See the sections on input bindings and output bindings below for information on required parameters.

```
[FunctionName("author")]
public static HttpResponseMessage author(HttpRequestMessage req,
    [HttpTrigger] AuthorRequest parameters,
    [SQLDatabase(ConnectionString = "ConnectionString",
                 SQLQuery = "SELECT * FROM Book WHERE Author LIKE CHAR(37)+'{name}'+CHAR(37)")] DataTable table,
    TraceWriter log)
{
    ...
}
```

## Input Binding

An input binding will perform a query and pass your function the query results as a DataTable. You can then work with the results by enumerating the DataRow items in the table.

### Parameters

Two parameters must be specified in the binding:
* ConnectionString: the setting name of your connection string, such as "ConnectionString". The setting and value must be set in your function project's local.settings.json file (when running locally) or in the Azure Portal under Application Settings for your Function App.
* SQLQuery: the SQL query to execute. You may embed a {parameter} name in the query, for example a query parameter from an HTTP Trigger, like this: SELECT CustId FROM Customer WHERE OrderNo={orderno}.

Because the binding framework interprets %name% as a reference to an app setting, you cannot use percent sign in your configured SQL queries. To work around this, use CHAR(37) in your queries.

The code example below, if invoked by HTTP with query parameter name=John+Smith would execute the query SELECT * FROM Book WHERE Author LIKE '%John Smith%'

## Supported Data Types

The [SQLDatabase] attribute should be followed by a variable. This can be either a DataTable or a string:

* DataTable: A System.Data.DataTable object. To access the data, enumerate the DataRow collection in the Rows property.
* String: A JSON serialization of the data table, of the form [ { "col1":, "value1", "col2":, "value2", ... }, { ...record 2... } ... { ...record N... } ]

````
    [SQLDatabase(ConnectionString = "ConnectionString",
                 SQLQuery = "SELECT * FROM Book WHERE Genre={genre}"] DataTable table,
    
    [FunctionName("author")]
public static HttpResponseMessage author(HttpRequestMessage req,
    [HttpTrigger] AuthorRequest parameters,
    [SQLDatabase(ConnectionString = "ConnectionString",
                 SQLQuery = "SELECT * FROM Book WHERE WHERE Genre={genre)"] string jsonTable,
````

## Code Example

```
// DataTable edition - input binding passes a DataTable object with the query results

[FunctionName("author")]
public static HttpResponseMessage author(HttpRequestMessage req,
    [HttpTrigger] AuthorRequest parameters,
    [SQLDatabase(ConnectionString = "ConnectionString",
                 SQLQuery = "SELECT * FROM Book WHERE Author LIKE CHAR(37)+'{name}'+CHAR(37)")] DataTable table,
    TraceWriter log)
{
    log.Info("author|DataTable: C# HTTP trigger function processed a request.");

    // Convert DataTable to JSON string

    var objType = JArray.FromObject(table, JsonSerializer.CreateDefault(new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })); //.FirstOrDefault(); // Get the first row            
    var js = objType.ToString();

    return req.CreateResponse(HttpStatusCode.OK, "{ Data: " + js + "}");
}
```

## Output Binding

An output binding will take the output of your function (a data table) and add records to the specified data table.

Records are added with SqlBulkCopy for high performance. Duplicate keys (records already in the table) are ignored and do not generate an error.

### Parameters

Two parameters must be specified in the binding:
* ConnectionString: the setting name of your connection string, such as "ConnectionString". The setting and value must be set in your function project's local.settings.json file (when running locally) or in the Azure Portal under Application Settings for your Function App.
* TableName: the database table to add records to.

```
[SQLDatabase(ConnectionString = "ConnectionString", TableName = "Book"] ICollector<DataTable> output
```

