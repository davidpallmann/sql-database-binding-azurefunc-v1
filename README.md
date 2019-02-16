# sql-database-binding-azurefunc-v1
Azure SQL Database binding for Azure Functions v1. (.NET Framework)

This project adds Input and Output Bindings for Azure SQL Database to Azure Functions v1 (.NET Framework). With it, cloud developers with Azure SQL Database databases can enjoy the same ease-of-use and minimal code in Azure Functions that other binding users enjoy.

# Usage

This binding is .NET Framework-based, and therefore is only compatible with v1 Azure Functions, such as C# .NET Framework functions developed in Visual Studio.

Your Visual Studio solution should contain your function app project plus this project or a reference to it.

In your C# function code, add a using statement for the SQLDatabaseExtension namespace

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

An input binding will perform a query and pass your function the query results as a DataTable. You can then work with the results by enumerating the DataRow items in the table. Or if you prefer, the data can be passed to your function as a JavaScript string.

### Parameters

Two parameters must be specified in the binding:
* ConnectionString: the setting name of your connection string, such as "ConnectionString". The setting and value must be set in your function project's local.settings.json file (when running locally) or in the Azure Portal under Application Settings for your Function App.
* SQLQuery: the SQL query to execute. You may embed a {parameter} name in the query, for example a query parameter from an HTTP Trigger, like this: SELECT CustId FROM Customer WHERE OrderNo={orderno}.

Because the binding framework interprets %name% as a reference to an app setting, you cannot use percent sign in your configured SQL queries. To work around this, use CHAR(37) in your queries.

The code example below, if invoked by HTTP with query parameter name=John+Smith would execute the query SELECT * FROM Book WHERE Author LIKE '%John Smith%'

### Supported Data Types

The [SQLDatabase] attribute should be followed by a variable. This can be either a DataTable or a string:

* DataTable: A System.Data.DataTable object. To access the data, enumerate the DataRow collection in the Rows property.
* String: A JavaScript serialization of the data table, in the form [ { "col1":, "value1", "col2":, "value2", ... }, { ...record 2... } ... { ...record N... } ]

````
    [SQLDatabase(ConnectionString = "ConnectionString",
                 SQLQuery = "SELECT * FROM Book WHERE Genre={genre}"] DataTable table,
    
    [SQLDatabase(ConnectionString = "ConnectionString",
                 SQLQuery = "SELECT * FROM Book WHERE WHERE Genre={genre)"] string jnTable,
````

### Code Examples

This example uses an HTTP Trigger and a SQL Database Input Binding that passes in a DataTable object:

```
public class AuthorRequest
{
    public string name { get; set; }
}
...

// DataTable edition - input binding passes a DataTable object with the query results

[FunctionName("author")]
public static HttpResponseMessage author(HttpRequestMessage req,
    [HttpTrigger] AuthorRequest parameters,
    [SQLDatabase(ConnectionString = "ConnectionString",
                 SQLQuery = "SELECT * FROM Book WHERE Author LIKE CHAR(37)+'{name}'+CHAR(37)")] DataTable table,
    TraceWriter log)
{
    log.Info("author|DataTable: C# HTTP trigger function processed a request.");

    // Convert DataTable to JS string

    var objType = JArray.FromObject(table, JsonSerializer.CreateDefault(new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })); //.FirstOrDefault(); // Get the first row            
    var js = objType.ToString();

    return req.CreateResponse(HttpStatusCode.OK, "{ Data: " + js + "}");
}
```
This example uses an HTTP Trigger and a SQL Database Input Binding that passes in a JavaScript string:

```
// JS edition - input binding passes a JavaScript string with the query results

[FunctionName("author")]
public static HttpResponseMessage author(HttpRequestMessage req,
    [HttpTrigger] AuthorRequest parameters,
    [SQLDatabase(ConnectionString = "ConnectionString",
                    SQLQuery = "SELECT * FROM Book WHERE Author LIKE CHAR(37)+'{name}'+CHAR(37)")] String jsonTable,
    TraceWriter log)
{
    log.Info("author|json: C# HTTP trigger function processed a request.");

    log.Info(jsonTable);

    return req.CreateResponse(HttpStatusCode.OK, "{ Data: " + jsonTable + "}");
}
```


## Output Binding

An output binding will take the output of your function (a DataTable object or JavaScript string) and add records to the specified data table.

Records are added with SqlBulkCopy for high performance. Duplicate keys (records already in the table) are ignored and do not generate an error.

### Parameters

Two parameters must be specified in the binding:
* ConnectionString: the setting name of your connection string, such as "ConnectionString". The setting and value must be set in your function project's local.settings.json file (when running locally) or in the Azure Portal under Application Settings for your Function App.
* TableName: the database table to add records to.

### Supported Data Types

The [SQLDatabase] attribute should be followed by an output variable that implements the ICollector interface. This can be either ICollector&lt;DataTable&gt; or ICollector&lt;string&gt;:

* ICollector&lt;DataTable&gt;: A System.Data.DataTable object. In your function, create the Data Table object and populate its Rows property wt DataRow objects.
* ICollector&lt;String&gt;: A JavaScript serialization of a DataTable, im the form [ { "col1":, "value1", "col2":, "value2", ... }, { ...record 2... } ... { ...record N... } ]

```
[SQLDatabase(ConnectionString = "ConnectionString", TableName = "Book"] ICollector<DataTable> output

[SQLDatabase(ConnectionString = "ConnectionString", TableName = "Book"] ICollector<string> output
```

In your C# function code, simply add one (or more) DataTables to the output variable with its Add method:

```
    DataTable dataTable = new DataTable() {
        ...
    };

    output.Add(dataTable);
```

### Code Examples

This example uses an HTTP trigger and a SQL Database Output Binding with an output variable of type DataTable. A book record is passed in the form of HTTP query parameters (title, author, yr, genre). A data table with one record is created and added to the output variable. After the code below executes the output binding adds the record to the Book table.

```
[FunctionName("addbook")]
public static HttpResponseMessage addbook(HttpRequestMessage req,
    [HttpTrigger] AddBookRequest parameters, 
    [SQLDatabase(ConnectionString = "ConnectionString", TableName = "Book"] ICollector<DataTable> output,
    TraceWriter log)
{
    // Validate/default parameters

    if (string.IsNullOrEmpty(parameters.title)) parameters.title = "Noname-" + System.Guid.NewGuid().ToString();
    if (string.IsNullOrEmpty(parameters.author)) parameters.author = null;
    if (string.IsNullOrEmpty(parameters.yr)) parameters.yr = null;
    if (string.IsNullOrEmpty(parameters.genre)) parameters.genre = null;

    // Create data table for output

    DataTable table = new DataTable();
    table.TableName = "Book";
    table.Clear();
    table.Columns.Add("Title");
    table.Columns.Add("Author");
    table.Columns.Add("Yr");
    table.Columns.Add("Genre");
    DataRow row = table.NewRow();
    row["Title"] = parameters.title;
    row["Author"] = parameters.author;
    row["Yr"] = parameters.yr;
    row["Genre"] = parameters.genre;
    table.Rows.Add(row);

    output.Add(table);

    return req.CreateResponse(HttpStatusCode.Created);
}

```

This example uses an HTTP trigger and a SQL Database Output Binding with an output variable of type string. A book record is passed in the form of HTTP query parameters (title, author, yr, genre). A string containing a JavaScript array with one record is created and added to the output variable. After the code below executes the output binding adds the record to the Book table.

```
/// JS edition - function passes a JavaScript string to the output binding with records to add

[FunctionName("addbook")]
public static HttpResponseMessage addbook(HttpRequestMessage req,
    [HttpTrigger] AddBookRequest parameters, 
    [SQLDatabase(ConnectionString = "ConnectionString",
                TableName = "Book", SQLQuery = "")] ICollector<string> output, TraceWriter log)
{
    // Validate/default parameters

    if (string.IsNullOrEmpty(parameters.title)) parameters.title = "Noname-" + System.Guid.NewGuid().ToString();
    if (string.IsNullOrEmpty(parameters.author)) parameters.author = null;
    if (string.IsNullOrEmpty(parameters.yr)) parameters.yr = null;
    if (string.IsNullOrEmpty(parameters.genre)) parameters.genre = null;

    // Create data table JS for output

    string json = @"[ { ""Title"": """ + parameters.title + @""", ""Author"": """ + parameters.author + @""", ""Yr"": """ + parameters.yr + @""", ""Genre"": """ + parameters.genre + @""" } ]";

    output.Add(json);

    return req.CreateResponse(HttpStatusCode.OK, "{ \"success\": \"1\" }");
}
```


