// Copyright © 2019 by David Pallmann. All rights reserved.
// Licensed under the MIT License. 

using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;

// SQLDatabase binding - attributes
// ConnectionString ................ database connection string (setting name) - 
//                                   (actual connection string is specified in local.settings.json or Azure Portal Application Settings)
// TableName ....................... table name (required for output bindings only)
// SQLQuery ........................ SQL query to execute (required for input binding only)

namespace SQLDatabaseExtension
{
    /// <summary>
    /// Binding attribute to place on user code for WebJobs. 
    /// </summary>
    [Binding]
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public class SQLDatabaseAttribute : Attribute
    {
        // Connection String (setting name)
        // Sample value: ConnectionString

        [AppSetting(Default="ConnectionString")]
        public string ConnectionString { get; set; }

        // Table name (output bindings - table to add to)
        // Sample value: Book

        [AutoResolve]
        public string TableName { get; set; }

        // SQL Query (input bindings - query to run)
        // Sample value: SELECT * FROM Book WHERE author={author}
        //
        // Note: the SQLQuery parmeter may not contain percent characters because the binding framework interprets them as appsetting names.
        // The work-around is to use char(37) in place of %, as in: ....WHERE title LIKE CHAR(37)+'{title}'+CHAR(37) 
        // which will resolve to:                                   ... WHERE title LIKE '%{title}%'

        [AutoResolve]
        public string SQLQuery { get; set; }
    }
}