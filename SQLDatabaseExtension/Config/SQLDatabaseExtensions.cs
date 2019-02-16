// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Config;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;

namespace SQLDatabaseExtension.Config
{
    /// <summary>
    /// Extension for binding <see cref="SQLDatabaseAttribute"/>.
    /// This binding queries or adds to SQL Server database tables, passing <see cref="DataTable"/> objects.
    /// </summary>

    public class SQLDatabaseExtensions : IExtensionConfigProvider
    {
        /// <summary>
        /// This callback is invoked by the WebJobs framework before the host starts execution. 
        /// It should add the binding rules and converters for our new <see cref="SQLDatabaseAttribute"/> 
        /// </summary>
        /// <param name="context"></param>
        public void Initialize(ExtensionConfigContext context)
        {
            // Register converters. These help convert between the user's parameter type
            //  and the type specified by the binding rules. 

            // This allows a user to bind to IAsyncCollector<string>, and the sdk
            // will convert that to IAsyncCollector<DataTable>
            context.AddConverter<string, DataTable>(Convert_String_to_DataTable);
            //context.AddConverter<JObject, DataTable>(Convert_JObject_to_DataTable);

            // This is useful on input. 
            context.AddConverter<DataTable, string>(Convert_DataTable_to_String);
            //context.AddConverter<DataTable, JObject>(Convert_DataTable_to_JObject);

            // Create 2 binding rules for the Sample attribute.
            var rule = context.AddBindingRule<SQLDatabaseAttribute>();

            rule.BindToInput<DataTable>(BuildItemFromAttr);
            rule.BindToCollector<string>(BuildJsonCollector);
            rule.BindToCollector<DataTable>(BuildCollector);
        }

        // Convert DataTable to JSON string

        private String Convert_DataTable_to_String(DataTable table)
        {
            return JsonConvert.SerializeObject(table, Formatting.Indented);
        }

        // Convert DataTable to JObject

        private JObject Convert_DataTable_to_JObject(DataTable table)
        {
            return JObject.Parse(JsonConvert.SerializeObject(table, Formatting.Indented));
        }

        // Convert JSON string to Data Table

        private DataTable Convert_String_to_DataTable(string json)
        {
            return JsonConvert.DeserializeObject<DataTable>(json);
        }

        // Convert JObject to Data Table

        private DataTable Convert_JObject_to_DataTable(JObject json)
        {
            return JsonConvert.DeserializeObject<DataTable>(json.ToString());
        }

        private IAsyncCollector<DataTable> BuildCollector(SQLDatabaseAttribute attribute)
        {
            var connectionString = attribute.ConnectionString; ;
            var tableName = attribute.TableName;
            return new SQLDatabaseAsyncDataTableCollector(connectionString, tableName);
        }

        private IAsyncCollector<string> BuildJsonCollector(SQLDatabaseAttribute attribute)
        {
            var connectionString = attribute.ConnectionString; ;
            var tableName = attribute.TableName;
            return new SQLDatabaseAsyncJsonCollector(connectionString, tableName);
        }

        // All {} and %% in the Attribute have been resolved by now. 
        private DataTable BuildItemFromAttr(SQLDatabaseAttribute attribute)
        {
            string connStr = attribute.ConnectionString;
            string tableName = attribute.TableName;
            string sqlQuery = attribute.SQLQuery;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                DataTable table = new DataTable();

                using (SqlCommand cmd = new SqlCommand(sqlQuery, conn))
                {
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(table);
                }

                conn.Close();

                return table;
            }
        }
    }
}
