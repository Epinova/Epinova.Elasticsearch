using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using EPiServer.Logging;

namespace Epinova.ElasticSearch.Core.Utilities
{
    public class DbHelper
    {
        private static readonly ILogger Log = LogManager.GetLogger(typeof(DbHelper));

        public static bool TableExists(string connectionString, string table)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    string sql = $"IF OBJECT_ID('{table}', 'U') IS NOT NULL SELECT 'true' ELSE SELECT 'false'";

                    using (var command = new SqlCommand(sql))
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
            catch (Exception exception)
            {
                Log.Warning($"Issue when checking if table '{table}' exists", exception);
                return false;
            }
        }

        public static void CreateTable(string connectionString, string table, string definition)
        {
            string sql = $"CREATE TABLE {table}( {definition} )";

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    using (var command = new SqlCommand(sql))
                    {
                        command.Connection = connection;
                        command.Connection.Open();
                        command.ExecuteScalar();
                        command.Connection.Close();

                        Log.Debug($"Table '{table}' created succesfully");
                    }
                }
            }
            catch (SqlException exception)
            {
                Log.Warning($"Issue when trying to create table '{table}'", exception);
            }
        }

        public static int ExecuteCommand(string connectionString, string sql, Dictionary<string, object> parameters = null)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    using (var command = new SqlCommand(sql))
                    {
                        if (parameters != null)
                        {
                            foreach (var kvp in parameters)
                            {
                                command.Parameters.Add(new SqlParameter(kvp.Key, kvp.Value));
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
            catch (SqlException exception)
            {
                Log.Warning("Issue when trying to execute command", exception);
                return 0;
            }
        }

        public static List<Dictionary<string, object>> ExecuteReader(string connectionString, string sql, Dictionary<string, object> parameters = null)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    using (var command = new SqlCommand(sql))
                    {
                        if (parameters != null)
                        {
                            foreach (var kvp in parameters)
                            {
                                command.Parameters.Add(new SqlParameter(kvp.Key, kvp.Value));
                            }
                        }

                        command.Connection = connection;
                        command.Connection.Open();
                        SqlDataReader reader = command.ExecuteReader();

                        var results = new List<Dictionary<string, object>>();

                        while (reader.Read())
                        {
                            var row = new Dictionary<string, object>();

                            for (var i = 0; i < reader.FieldCount; i++)
                            {
                                row.Add(reader.GetName(i), reader.GetValue(i));
                            }

                            results.Add(row);
                        }

                        command.Connection.Close();

                        Log.Debug($"Command '{sql}' executed succesfully.");

                        return results;
                    }
                }
            }
            catch (SqlException exception)
            {
                Log.Warning("Issue when trying to execute command", exception);
                return null;
            }
        }
    }
}