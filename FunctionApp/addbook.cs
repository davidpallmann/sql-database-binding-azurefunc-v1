// Comment out the line below for a version of this code that passes a DataTable to the output binding
// Uncomment the line below for a version of this code that is passes a JSON string to the output binding
//#define JSON

using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using SQLDatabaseExtension;

namespace BookFunctionApp
{
    public class AddBookRequest
    {
        public string title { get; set; }
        public string author { get; set; }
        public string yr { get; set; }
        public string genre { get; set; }
    }

    public static class AddbookFunction
    {
        // Function: addbook - adds a new book record (duplicate key is ignored)
        // Inputs:         title  ...... book title
        //                 author  ..... author name
        //                 yr  ......... year published
        //                 genre  ...... genre
        // Output:         <result> .... HttpStatusCode.Created on success
        // Input Example:  http://localhost:7071/api/addbook?title=Under+the+Grandstands&author=Seymour+Butts&yr=1976&genre=Tomfoolery

#if JSON
        // JSON edition - function passes a JSON string to the output binding with records to add

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

            // Create data table JSON for output

            string json = @"[ { ""Title"": """ + parameters.title + @""", ""Author"": """ + parameters.author + @""", ""Yr"": """ + parameters.yr + @""", ""Genre"": """ + parameters.genre + @""" } ]";

            output.Add(json);

            return req.CreateResponse(HttpStatusCode.Created);
        }
#else
        // DataTable edition - function passes a DataTable object to the output binding with records to add

        [FunctionName("addbook")]
        public static HttpResponseMessage addbook(HttpRequestMessage req,
            [HttpTrigger] AddBookRequest parameters, 
            [SQLDatabase(ConnectionString = "ConnectionString", TableName = "Book")] ICollector<DataTable> output,
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
#endif
    }
}