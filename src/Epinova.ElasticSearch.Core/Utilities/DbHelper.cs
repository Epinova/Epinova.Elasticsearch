using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using EPiServer.Logging;

namespace Epinova.ElasticSearch.Core.Utilities
{
    public static class DbHelper
    {
        private static readonly ILogger Log = LogManager.GetLogger(typeof(DbHelper));

        public static bool ColumnExists(string connectionString, string table, string column, string schema = "dbo")
        {
            try
            {
                using(var connection = new SqlConnection(connectionString))
                {
                    var sql = $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE [TABLE_SCHEMA] = '{schema}' AND [TABLE_NAME] = '{table}' AND [COLUMN_NAME] = '{column}'";

                    using(var command = new SqlCommand(sql))
                    {
                        command.Connection = connection;
                        command.Connection.Open();
                        object result = command.ExecuteScalar();
                        command.Connection.Close();

                        var count = Convert.ToInt32(result);

                        if(count > 0)
                        {
                            Log.Debug($"Column '{schema}.{table}.{column}' exists");
                            return true;
                        }

                        Log.Debug($"Column '{schema}.{table}.{column}' does NOT exist");
                        return false;
                    }
                }
            }
            catch(Exception exception)
            {
                Log.Warning($"Issue when checking if column '{schema}.{table}.{column}' exists", exception);
                return false;
            }
        }

        public static bool TableExists(string connectionString, string table)
        {
            try
            {
                using(var connection = new SqlConnection(connectionString))
                {
                    var sql = $"IF OBJECT_ID('{table}', 'U') IS NOT NULL SELECT 'true' ELSE SELECT 'false'";

                    using(var command = new SqlCommand(sql))
                    {
                        command.Connection = connection;
                        command.Connection.Open();
                        object result = command.ExecuteScalar();
                        command.Connection.Close();
                        Boolean.TryParse((string)result, out bool exists);

                        Log.Debug($"Table '{table}' exists: {exists}");

                        return exists;
                    }
                }
            }
            catch(Exception exception)
            {
                Log.Warning($"Issue when checking if table '{table}' exists", exception);
                return false;
            }
        }

        public static void CreateTable(string connectionString, string table, string definition)
        {
            var sql = $"CREATE TABLE {table}( {definition} )";

            try
            {
                using(var connection = new SqlConnection(connectionString))
                {
                    using(var command = new SqlCommand(sql))
                    {
                        command.Connection = connection;
                        command.Connection.Open();
                        command.ExecuteScalar();
                        command.Connection.Close();

                        Log.Debug($"Table '{table}' created succesfully");
                    }
                }
            }
            catch(SqlException exception)
            {
                Log.Warning($"Issue when trying to create table '{table}'", exception);
            }
        }

        public static int ExecuteCommand(string connectionString, string sql, Dictionary<string, object> parameters = null)
        {
            try
            {
                Log.Debug($"ExecuteCommand: '{sql}");

                using(var connection = new SqlConnection(connectionString))
                {
                    using(var command = new SqlCommand(sql))
                    {
                        if(parameters != null)
                        {
                            foreach(KeyValuePair<string, object> kvp in parameters)
                            {
                                command.Parameters.Add(new SqlParameter(kvp.Key, kvp.Value));
                            }

                            if(Log.IsDebugEnabled())
                            {
                                Log.Debug($"Parameters:\n {String.Join("\n", parameters.Select(p => p.Key + "=" + p.Value))}");
                            }
                        }

                        command.Connection = connection;
                        command.Connection.Open();
                        int rowsAffected = command.ExecuteNonQuery();
                        command.Connection.Close();

                        Log.Debug($"Command '{sql}' executed succesfully. {rowsAffected} rows affected.");

                        return rowsAffected;
                    }
                }
            }
            catch(SqlException exception)
            {
                Log.Warning("Issue when trying to execute command", exception);
                return 0;
            }
        }

        public static List<Dictionary<string, object>> ExecuteReader(string connectionString, string sql, Dictionary<string, object> parameters = null)
        {
            var results = new List<Dictionary<string, object>>();

            try
            {
                Log.Debug($"ExecuteReader: '{sql}");

                using(var connection = new SqlConnection(connectionString))
                {
                    using(var command = new SqlCommand(sql))
                    {
                        if(parameters != null)
                        {
                            foreach(var kvp in parameters)
                            {
                                command.Parameters.Add(new SqlParameter(kvp.Key, kvp.Value));
                            }

                            if(Log.IsDebugEnabled())
                            {
                                Log.Debug($"Parameters:\n {String.Join("\n", parameters.Select(p => p.Key + "=" + p.Value))}");
                            }
                        }

                        command.Connection = connection;
                        command.Connection.Open();
                        SqlDataReader reader = command.ExecuteReader();

                        while(reader.Read())
                        {
                            var row = new Dictionary<string, object>();

                            for(var i = 0; i < reader.FieldCount; i++)
                            {
                                row.Add(reader.GetName(i), reader.GetValue(i));
                            }

                            results.Add(row);
                        }

                        command.Connection.Close();

                        Log.Debug($"Command '{sql}' executed succesfully.");
                    }
                }
            }
            catch(SqlException exception)
            {
                Log.Warning("Issue when trying to execute command", exception);
            }

            return results;
        }
    }
}