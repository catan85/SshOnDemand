//-----------------------------------------------------------------------
// <copyright file="PostgreSQLClass.cs" company="Marzoli">
//     Copyright (c) Marzoli. All rights reserved.
// </copyright>
// <author>Andrea Cattaneo</author>
//-----------------------------------------------------------------------
namespace ApiServer
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading;
    using Npgsql;
    using System.Text;
    using System.Configuration;
    using System.Linq;

    /// <summary>
    /// Classe di interfacciamento con database Postgresql
    /// </summary>
    public static class PostgreSQLClass
    {

        private static string connectionString = "";
        private static int commandTimeout = 60;
        private static NpgsqlConnection DbConnection;

        public static DataTable GetClientsDatatable(out bool fault)
        {
            string query =
                            $@"SELECT 
                                client_name, client_key 
                            FROM 
                                clients;";
            string tablename = "clients";

            return GetDatatable(query, tablename, out fault);
        }



        public static bool IsDeveloperConnectionToDeviceAuthorized(string developerName, string deviceName, out bool fault)
        {
            string query = $@"select developers.client_name as developer_name , devices.client_name as device_name from developer_authorizations da 
                            join clients devices on da.device_id = devices.id
                            join clients as developers on da.developer_id = developers.id
                            where developers.client_name = '{developerName}' and devices.client_name = '{deviceName}';";

            DataTable dt = GetDatatable(query, "developer_authorizations", out fault);

            if (dt.Rows.Count == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void InsertDeviceConnectionRequest(string deviceName, bool isRequest, out bool fault)
        {
            string query =
                            $@" BEGIN;
								INSERT INTO device_requests (client_id, is_requested, request_timestamp ) 
                                SELECT clients.id, { (isRequest ? "true" : "false") }, '{CurrentTimestampString()}'
                                FROM clients where client_name = '{deviceName}'
                                AND NOT EXISTS ( select true from device_requests where client_id = clients.id );

                                UPDATE device_requests
                                SET is_requested = { (isRequest ? "true" : "false") },
                                    request_timestamp = '{CurrentTimestampString()}'
                                FROM clients where client_name = '{deviceName}'
                                and client_id = clients.id;
                                COMMIT;
                            ";

            QueryDatabase(query, out fault);
        }

        public static int CheckDeviceConnection(string deviceName, out bool fault)
        {
            int status = 0;
            string query = $@"SELECT status FROM client_connections
                            JOIN clients 
                            ON client_connections.client_id = clients.id
                            WHERE clients.client_name = '{deviceName}';";
            DataTable data = GetDatatable(query, "connections", out fault);

            if (data.Rows.Count == 1)
            {
                status = (int)data.Rows[0]["status"];
            }
            return status;
        }

        public static void DeactivateOldRequests(int maxRequestAgeInSeconds, out bool fault)
        {
            string timeLimit = (DateTime.UtcNow - new TimeSpan(0, 0, maxRequestAgeInSeconds)).ToString(Constants.DATETIME_FORMAT_STRING);
            string query = $"update device_requests set is_requested = false where request_timestamp < '{timeLimit}' and is_requested = true;";
            QueryDatabase(query, out fault);
        }

        public static void ResetOldConnections(int maxConnectionAgeInSeconds, out bool fault)
        {
            string timeLimit = (DateTime.UtcNow - new TimeSpan(0, 0, maxConnectionAgeInSeconds)).ToString(Constants.DATETIME_FORMAT_STRING);
            string query = $"update client_connections set status = 0 where connection_timestamp < '{timeLimit}' and status != 0;";
            QueryDatabase(query, out fault);
        }

        private static string CurrentTimestampString()
        {
            return DateTime.UtcNow.ToString(Constants.DATETIME_FORMAT_STRING);
        }

        /// <summary>
        /// Apertura di una connessione al database
        /// </summary>
        /// <param name="fault">Erorre nell'apertura della connessione</param>
        private static void OpenConnection(ref NpgsqlConnection currentConnection, out bool fault)
        {
            InitConnectionString();
            fault = false;
            if (currentConnection == null || currentConnection.State != ConnectionState.Open)
            {
                if (currentConnection != null)
                {
                    currentConnection.Dispose();
                    currentConnection = null;
                }

                currentConnection = new NpgsqlConnection(connectionString);
                currentConnection.Open();
            }
        }
        private static void InitConnectionString()
        {
            if (connectionString == "")
            {

                connectionString =
                    "Server=" + AppSettings.DbHost +
                    ";Port=" + AppSettings.DbPort.ToString() +
                    ";Database=" + AppSettings.DbName +
                    ";User Id=" + AppSettings.DbUser +
                    ";Password=" + AppSettings.DbPass +
                    ";Integrated Security=true; Timeout=60;";
            }
        }

        private static DataTable GetDatatable(string query, string tablename, out bool fault)
        {
            DataTable result = null;

            try
            {
                OpenConnection(ref DbConnection, out fault);

                if (!fault)
                {
                    string commandString = "";

                    commandString = query;

                    result = new DataTable(tablename);

                    GetTableFromDB(DbConnection, ref result, commandString, true);

                    return result;
                }
            }
            catch (Exception ex)
            {
                string errorString = "Postgres database connection error";

                Console.WriteLine(errorString);
                Console.WriteLine(ex.Message);
                fault = true;
                try
                {
                    DbConnection.Close();
                }
                catch { }
            }

            return result;
        }

        private static void GetTableFromDB(NpgsqlConnection currentConnection, ref DataTable table, string selectString, bool setPrimaryKey)
        {
           
           bool connectionFault = false;
           if (currentConnection.State != ConnectionState.Open)
           {
               OpenConnection(ref currentConnection, out connectionFault);
           }

           if (!connectionFault)
           {
               string tablename = table.TableName;

               using (NpgsqlCommand command = new NpgsqlCommand(selectString, currentConnection))
               {
                   command.CommandTimeout = commandTimeout;
                   using (NpgsqlDataReader reader = command.ExecuteReader())
                   {
                       DataTable tempTab = new DataTable(tablename);
                       while (reader.Read())
                       {
                           object[] vals = new object[reader.FieldCount];
                           if (tempTab.Columns.Count == 0)
                           {
                               for (int i = 0; i < reader.FieldCount; i++)
                               {
                                   tempTab.Columns.Add(reader.GetName(i), reader.GetFieldType(i));
                               }
                           }
                           reader.GetValues(vals);
                           tempTab.Rows.Add(vals);
                       }

                       table = tempTab;
                   }
               }

               if (table.PrimaryKey != null && setPrimaryKey && table.Columns.Count > 0)
               {
                   table.PrimaryKey =
                       new DataColumn[] { table.Columns[0] };
               }
           }

        }

        private static void QueryDatabase(string query, out bool fault)
        {
            try
            {
                OpenConnection(ref DbConnection, out fault);

                if (!fault)
                {
                    string commandString = query;

                    using (NpgsqlCommand command = new NpgsqlCommand(commandString, DbConnection))
                    {
                        command.CommandTimeout = commandTimeout;
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                string errorString = "Postgres database connection error";

                Console.WriteLine(errorString);
                Console.WriteLine(ex.Message);
                fault = true;
                try
                {
                    DbConnection.Close();
                }
                catch { }
            }
        }
   
    }
}