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
            string query = $@"select developers.client_name , devices.client_name from developer_authorizations da 
                            join clients devices on da.device_id = devices.id
                            join clients as developers on da.developer_id = developers.id";

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
                            $@" BEGIN
								INSERT INTO device_requests (client_id, is_requested, request_timestamp ) 
                                SELECT clients.id, { (isRequest ? "true" : "false") }, '{DateTime.UtcNow.ToString(Constants.DATETIME_FORMAT_STRING)}'
                                FROM clients where client_name = '{deviceName}'
                                AND NOT EXISTS ( select true from device_requests where client_id = clients.id );

                                UPDATE device_requests
                                SET is_requested = { (isRequest ? "true" : "false") },
                                    request_timestamp = '{DateTime.UtcNow.ToString(Constants.DATETIME_FORMAT_STRING)}'
                                FROM clients where client_name = '{deviceName}'
                                and client_id = clients.id;
                                COMMIT;
                            ";

            QueryDatabase(query, out fault);
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