// Comment out the line below for a version of this code that is passed a DataTable by the input binding
// Uncomment the line below for a version of this code that is passed a JSON string by the input binding
//#define JSON

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using SQLDatabaseExtension;
using System.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BookFunctionApp
{
    public class TitleRequest
    {
        public string title { get; set; }
    }

    public static class titleFunction
    {
        // Function: title - queries books matching full or partial book title
        // Inputs:         title........ title or partial title
        // Output:         <result> .... array of DataRow values from querying Book table
        // Input Example:  http://localhost:7071/api/title?title=proteus
        // Output Example: Data: [ { "Title": "Proteus in the Underworld", "Author": "Charles Sheffield", "Yr": "1995", "Genre": "Science Fiction" }, { "Title": "Sight of Proteus", "Author": "Charles Sheffield", "Yr": "1978", "Genre": "Science Fiction" } ]

#if JSON
        // JSON edition - input binding passes a JSON string with the query results

        [FunctionName("title")]
        public static HttpResponseMessage title(HttpRequestMessage req,
            [HttpTrigger] TitleRequest parameters, 
            [SQLDatabase(ConnectionString = "ConnectionString",
                         SQLQuery = "SELECT * FROM Book WHERE Title LIKE +CHAR(37)+'{title}'+CHAR(37)")]
                string jsonTable,
            TraceWriter log)
        {
            log.Info("title|json: C# HTTP trigger function processed a request.");

            //log.Info(jsonTable);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{ \"Data\": " + jsonTable + " }", Encoding.UTF8, "application/json")
            };
        }
#else
        // DataTable edition - input binding passes a DataTable object with the query results

        [FunctionName("title")]
        public static HttpResponseMessage title(HttpRequestMessage req,
            [HttpTrigger] TitleRequest parameters, 
            [SQLDatabase(ConnectionString = "ConnectionString",
                         SQLQuery = "SELECT * FROM Book WHERE Title LIKE +CHAR(37)+'{title}'+CHAR(37)")]
                DataTable table,
            TraceWriter log)
        {
            log.Info("title|DataTable: C# HTTP trigger function processed a request.");

            // Convert data table to JSON string

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