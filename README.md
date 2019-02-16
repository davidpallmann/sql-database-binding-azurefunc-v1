# sql-database-binding-azurefunc-v1
Azure SQL Database binding for Azure Functions v1. (.NET Framework)

This project adds Input and Output Bindings for Azure SQL Database to Azure Functions v1 (.NET Framework). With it, cloud developers with Azure SQL Database databases can enjoy the same ease-of-use and minimal code in Azure Functions that other binding users enjoy.

For building/running locally/publishing to Azure, see the Deployment section further below.

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
                    SQLQuery = "SELECT * FROM Book WHERE Author LIKE CHAR(37)+'{name}'+CHAR(37)")] String jsTable,
    TraceWriter log)
{
    log.Info("author|js: C# HTTP trigger function processed a request.");

    log.Info(jsTable);

    return req.CreateResponse(HttpStatusCode.OK, "{ Data: " + jsTable + "}");
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

# Deployment


## Building

1. Before working with this code, you should already be set up to use Azure Functions in Visual Studio 2017, with the necessary tools installed (including Tools > Extensions and Updates | Azure Functions and Web Jobs Tools).

2. Once you have downloaded or cloned this repository, open the solution SQLDatabaseExtension.sln in Visual Studio 2017.

3. Build the solution. If you receive any errors that suggest you are missing NuGet packages, try right-clicking the Solution in Solution Explorer and selecting Restore NuGet Packages.

## Running the Included Book Sample

1. If you want to run the included sample, you will need to first do the following:

   * Create an Azure SQL Database named Books.

   * In SSMS, connect to the database and run the included script, CreateBookDatabase.sql. This will add a table named Book along with some data records.

   * In the FunctionApp project in Visual Studio, edit the local.settings.json file and set the ConnectionString value to a connection string for your Books database.

2. Switch to Debug configuration.

3. Make sure **FunctionApp** is the start-up project (if unsure, right-click FunctionApp in Solution Explorer and select Set as Startup Project).

4. Press F5 to run.

5. A console window should open, with a lightning bold text logo at the top.

6. Once the function app initializes, you should see "Http Functions" and a list of functions and URLs similar to the following:


```
Http Functions:

        addbook: http://localhost:7071/api/addbook

        addbooks: http://localhost:7071/api/addbooks

        author: http://localhost:7071/api/author

        title: http://localhost:7071/api/title

Debugger listening on [::]:5858
```

7. Go to a browser and enter one of the following (be sure to use the base URLs that were displayed in Step 7)

   * Title Search

http://localhost:7071/api/title?title=progam should list books with "program" in their title
http://localhost:7071/api/title?title=world should list books with "world" in their title

   * Author Search

http://localhost:7071/api/author?name=smith should list books written by Cordwainer Smith
http://localhost:7071/api/author?name=pallmann should list books written by David Pallmann

   * Add a Book

http://localhost:7071/api/addbook?title=My+Binding+Works&author=Me&yr=2019&genre=NonFiction should add a new book title.

   * Add Multiple Books

For this test you will need something capable of sending an HTTP Post, such as Insomnia.

Post the following body to the addbooks URL http://localhost:7071/api/addbooks

```
       [
          {
             "title": "Rendezvous with Rama",
             "author": "Arthur C. Clarke",
             "yr": "1973",
             "genre": "Science Fiction"
          },
          {
             "title": "Childhood's End",
             "author": "Arthur C. Clarke",
             "yr": "1953",
             "genre": "Science Fiction"
          },
          {
             "title": "2001: A Space Odyssey",
             "author": "Arthur C. Clarke",
             "yr": "1968",
             "genre": "Science Fiction"
          },
		  {
             "title": "The Songs of Distant Earth",
             "author": "Arthur C. Clarke",
             "yr": "1986",
             "genre": "Science Fiction"
          },
		  {
             "title": "Imperial Earth",
             "author": "Arthur C. Clarke",
             "yr": "1975",
             "genre": "Science Fiction"
           },
		   {
             "title": "The Sentinel",
             "author": "Arthur C. Clarke",
             "yr": "1951",
             "genre": "Science Fiction"
           }
        ]
```

New books should have been added, which you can see using the title or author search URLS mentioned earlier. Or, in SQL Server Management Studio issue thi query: SELECT * FROM Book

## Adding SQLDatabase Binding To Your FunctionApp Project

1. Open your Function App solution in Visual Studio

2. Add the SQLDatabaseExtension project to your solution; or add a reference to SQLDatabaseExtension.dll.

3. Add [SqlDatabase] input or output bindings to suit your needs, as described earlier under Usage.

4. Configure your ConnectionString setting in local.settings.json.

5. Test your function locally with the SQLDatabase binding unti your are satisfied.

## Publishing to Azure

1. These instructions assume you already have done the following:
a. Created an Azure Function in the Azure Portal and set the runtime to v1.
b. Downloaded a Publishing Profile.
c. Have added SQLDatabase bindings to your function and tested locally.

2. In the Azure Portal, select your function app and go to Application Settings. Add a ConnectionString setting for your database.

3. Publish your function. 

4. Try testing you function in the cloud.
