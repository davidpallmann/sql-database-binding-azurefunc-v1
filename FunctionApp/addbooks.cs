// Comment out the line below for a version of this code that passes a DataTable to the output binding
// Uncomment the line below for a version of this code that is passes a JSON string to the output binding
#define JSON

using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using SQLDatabaseExtension;

namespace BookFunctionApp
{
    public class Book
    {
        public string title { get; set; }
        public string author { get; set; }
        public string yr { get; set; }
        public string genre { get; set; }
    }

    public static class AddbooksFunction
    {
        // Function: addbooks - adds mutiple book records (any records with duplicate keys are ignored)
        // Inputs:         title  ...... book title
        //                 author  ..... author name
        //                 yr  ......... year published
        //                 genre  ...... genre
        // Output:         <result> .... HTTP 201 Created>
        // Input Example:  POST http://localhost:7071/api/addbooks
        // [
        //   {
        //      "title": "In the Dark",
        //      "author": "Richard Schaeffer",
        //      "yr": "1843",
        //      "genre": "Fiction"
        //    },
        //    {
        //      "title": "In the Sunshine",
        //      "author": "George Wakefield",
        //      "yr": "1953",
        //      "genre" "NonFiction"
        //    }
        // ]

#if JSON
        // JSON edition - function passes a JSON string with multiple rows to the output binding with records to add

        [FunctionName("addbooks")]
        public static HttpResponseMessage addbooks(HttpRequestMessage req, 
            [HttpTrigger] Book[] books, 
            [SQLDatabase(ConnectionString = "ConnectionString",
                        TableName = "Book", SQLQuery = "")] ICollector<string> output, TraceWriter log)
        {
             // Create data table JSON for output

            string json = @"[ ";
            if (books != null)
            {
                int count = 0;
                foreach (Book book in books)
                {
                    if (count > 0) json += ",";
                    json += @"{ ""Title"": """ + book.title + @""", ""Author"": """ + book.author + @""", ""Yr"": """ + book.yr + @""", ""Genre"": """ + book.genre + @""" }";
                    count++;
                }
            }
            json += "]";

            output.Add(json);

            return req.CreateResponse(HttpStatusCode.Created);
        }
#else
        // DataTable edition - function passes a DataTable object with multiple rows to the output binding with records to add

        [FunctionName("addbooks")]
        public static HttpResponseMessage addbooks(HttpRequestMessage req, 
            [HttpTrigger] Book[] books, 
            [SQLDatabase(ConnectionString = "ConnectionString",
                        TableName = "Book", SQLQuery = "")] ICollector<DataTable> output, TraceWriter log)
        {
             // Create data table for output

            DataTable dt = new DataTable();
            dt.TableName = "Book";
            dt.Clear();
            dt.Columns.Add("Title");
            dt.Columns.Add("Author");
            dt.Columns.Add("Yr");
            dt.Columns.Add("Genre");
            DataRow row = null;

            if (books != null)
            {
                foreach (Book book in books)
                {
                    row = dt.NewRow();
                    row["Title"] = book.title;
                    row["Author"] = book.author;
                    row["Yr"] = book.yr;
                    row["Genre"] = book.genre;
                    dt.Rows.Add(row);
                }
            }

            output.Add(dt);

            return req.CreateResponse(HttpStatusCode.Created);
        }
#endif
    }
}