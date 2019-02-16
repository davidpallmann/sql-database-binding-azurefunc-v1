// Comment out the line below for a version of this code that is passed a DataTable by the input binding
// Uncomment the line below for a version of this code that is passed a JSON string by the input binding
//#define JSON

using System;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using SQLDatabaseExtension;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BookFunctionApp
{
    public class AuthorRequest
    {
        public string name { get; set; }
    }

    public static class AuthorFunction
    {
        // Function: author - queries books matching full or partial author name
        // Inputs:         name........ name or partial name
        // Output:         <result> .... array of DataRow values from querying Book table
        // Input Example:  http://localhost:7071/api/author?name=sheffield
        // Output Example: Data: [ { "Title": "Proteus in the Underworld", "Author": "Charles Sheffield", "Yr": "1995", "Genre": "Science Fiction" }, { "Title": "Sight of Proteus", "Author": "Charles Sheffield", "Yr": "1978", "Genre": "Science Fiction" } ]
#if JSON
        // JSON edition - input binding passes a JSON string with the query results

        [FunctionName("author")]
        public static HttpResponseMessage author(HttpRequestMessage req,
            [HttpTrigger] AuthorRequest parameters,
            [SQLDatabase(ConnectionString = "ConnectionString",
                         SQLQuery = "SELECT * FROM Book WHERE Author LIKE CHAR(37)+'{name}'+CHAR(37)")] String jsonTable,
            TraceWriter log)
        {
            log.Info("author|json: C# HTTP trigger function processed a request.");

            log.Info(jsonTable);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{ \"Data\": " + jsonTable + " }", Encoding.UTF8, "application/json")
            };
        }
#else
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

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{ \"Data\": " + js + " }", Encoding.UTF8, "application/json")
            };
        }
#endif
    }
}