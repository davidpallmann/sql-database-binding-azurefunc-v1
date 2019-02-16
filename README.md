# SQLDatabase Binding for Azure Functions v1

This is a binding for Azure Functions for Azure SQL Database. With it, cloud developers who use SQL Database can enjoy the same benefits that other binding users enjoy, includining minimal function code.

This project builds SQLBindingExtension.dll which defines a new [SQLDatabase] binding attribute.

This binding is for the Azure Functions v1 runtime (.NET Framework).

For building and deployment instructions, see Deployment further down.

# Usage

To use the SQL Database binding, just add a [SQLDatabase] attribute to your functions.

```
[FunctionName("genre")]
public static HttpResponseMessage title(HttpRequestMessage req,
    [HttpTrigger] TitleRequest parameters, 
    [SQLDatabase(ConnectionString = "ConnectionString", SQLQuery = "SELECT * FROM Book WHERE Genre={genre}] DataTable table,
    TraceWriter log)
{
    // Convert data table to JSON string

    var objType = JArray.FromObject(table, JsonSerializer.CreateDefault(new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })); //.FirstOrDefault(); // Get the first row            
    var js = objType.ToString();

    return new HttpResponseMessage(HttpStatusCode.OK)
    {
        StatusCode = HttpStatusCode.OK,
        Content = new StringContent("{ \"Data\": " + js + " }", Encoding.UTF8, "application/json")
    };
}
```

For specific details on parameters, see Input Bindings and Output Bindings.

## Input Bindings

When used as an input binding, the binding executes a query and passes the result to your function--either as a System.Data.DataTable object or the equivalent JSON string.

### Parameters

Two parameters are required for an input binding:

* ConnnectionString: Name of connection string setting (such as "ConnectionString"). The actual connection string should be defined in local.settings.json (if running locally) or Application Settings in the Azure Portal.
* SQLQuery: A T-SQL query to execute. The results will be passed to your function.

```
[SQLDatabase(ConnectionString = "ConnectionString", SQLQuery = "SELECT * FROM Book WHERE Genre={genre}] DataTable table,
```

SQLQuery parameters may contain {parameter} names. For example, an HTTP Trigger may pass in a query string parameter named title. If your SQLQuery parameter contains {title} it will be replaced with the incoming parameter value. 

Important: The percent sign (%) character cannot be used in the SQLQuery parameter. This is because the Azure Functions binding framework interprets %name% as application setting references. However, there is a work-around. Use CHAR(37) in place of %. 

For example,

...WHERE title LIKE CHAR(37) + '{title}' + CHAR(37)

will execute as

...WHERE title LIKE '%{title}%'

### Supported Data Types

Your [SQLDatabase] attribute should be followed by a variable, which can be either of these types:

* DataTable: a System.Data.DataTable. You can enumerate the DataRow collection in the Rows property.
* string: a JSON string - a serialized DataTable (actually a fragment, not complete JSON) in the form
```
[ { "column1": "value1", "column2": "value2", ... }, { ...record 2...}, ... { ...record N... } ]
```

Use whichever data type suits your work best.

```
[SQLDatabase(ConnectionString = "ConnectionString", SQLQuery = "SELECT * FROM Book WHERE Genre={genre}] DataTable table,

[SQLDatabase(ConnectionString = "ConnectionString", SQLQuery = "SELECT * FROM Book WHERE Genre={genre}] string tableJson,

```

### Code Example: Input Binding

The example below performs a title search







https://github.com/davidpallmann/sql-database-binding-azurefunc-v1

For details on installing and using, see the github readme.

This sample contains 2 projects:

1. SQLDatabaseExtension: this is the binding itself, which adds a [SqlDatabase] binding to Azure Functions.
2. FunctionApp: an app for testing/demonstrating the SQLDatabase binding.

For creating the sample Book table and data records for the sample, create an Azure SQL Database, connect to it in SSMS, and run the script CreateBooksDatabase.sql


