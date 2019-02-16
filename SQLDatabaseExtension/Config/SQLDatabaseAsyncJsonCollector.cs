// Copyright © 2019 by David Pallmann. All rights reserved.
// Licensed under the MIT License. 

using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SQLDatabaseExtension.Config
{
    /// <summary>
    /// This class implements output binding for a JSON string collection. The JSON string must be a serialized DataTable [ { "col1": "value1", ... } ]
    /// Requires ConnectionString and TableName parameters. 
    /// </summary>
    internal class SQLDatabaseAsyncJsonCollector : IAsyncCollector<string>
    {
        private readonly string ConnectionString;   // Database connection string
        private readonly string TableName;          // Database table name

        public SQLDatabaseAsyncJsonCollector(string connectionString, string tableName)
        {
            ConnectionString = connectionString;
            TableName = tableName;
        }

        // Add database records from output binding

        public Task AddAsync(string json, CancellationToken cancellationToken = default(CancellationToken))
        {
            DataTable dt = JsonConvert.DeserializeObject<DataTable>(json);
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                if (dt.Rows.Count > 0)
                {
                    using (SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(conn))
                    {
                        // Bulk insert DataTable rows into database
                        sqlBulkCopy.DestinationTableName = TableName; 
                        conn.Open();
                        sqlBulkCopy.WriteToServer(dt);
                        conn.Close();
                    }
                }
            }

            return Task.CompletedTask;
        }

        public Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.CompletedTask;
        }
    }

}