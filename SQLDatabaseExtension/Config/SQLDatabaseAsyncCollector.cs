// Copyright © 2019 by David Pallmann. All rights reserved.
// Licensed under the MIT License. 

using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace SQLDatabaseExtension.Config
{
    /// <summary>
    /// This class implements output binding for a DataTable collection. 
    /// Requires ConnectionString and TableName parameters. 
    /// </summary>
    internal class SQLDatabaseAsyncDataTableCollector : IAsyncCollector<DataTable>
    {
        private readonly string ConnectionString;   // Database connection string
        private readonly string TableName;          // Database table name

        public SQLDatabaseAsyncDataTableCollector(string connectionString, string tableName)
        {
            ConnectionString = connectionString;
            TableName = tableName;
        }

        // Add database records from output binding

        public Task AddAsync(DataTable dt, CancellationToken cancellationToken = default(CancellationToken))
        {
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