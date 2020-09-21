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
    using SshOnDemandLibs;

    /// <summary>
    /// Classe di interfacciamento con database Postgresql
    /// </summary>
    public static class PostgreSQLClass
    {

        private static string connectionString = "";
        private static int commandTimeout = 60;
        private static NpgsqlConnection DbConnection;

        #region common
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

        public static DeviceConnectionStatus CheckDeviceConnection(string deviceName, out bool fault)
        {
            DeviceConnectionStatus status = new DeviceConnectionStatus();
            status.State = ClientConnectionState.Disconnected;

            string query = $@"SELECT status, ssh_ip, ssh_port, ssh_user, ssh_forwarding FROM client_connections
                            JOIN clients 
                            ON client_connections.client_id = clients.id
                            WHERE clients.client_name = '{deviceName}';";
            DataTable data = GetDatatable(query, "connections", out fault);

            if (data.Rows.Count == 1)
            {
                status.State = (ClientConnectionState)((short)data.Rows[0]["status"]);
                status.SshHost = (string)data.Rows[0]["ssh_ip"];
                status.SshPort = (int)data.Rows[0]["ssh_port"];
                status.SshForwarding = (int)data.Rows[0]["ssh_forwarding"];
                status.SshUser = (string)data.Rows[0]["ssh_user"];
            }
            return status;
        }
        #endregion

        #region device queries
        public static bool IsDeviceConnectionAuthorized(string deviceName, out bool fault)
        {
            string query = $@"select true from clients where client_name = '{deviceName}';";

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


        public static bool IsDeviceConnectionRequested(string deviceName, out bool fault)
        {
            string query = $@"select is_requested from device_requests
                            join clients on clients.id = device_requests.client_id
                            where clients.client_name = '{deviceName}';";

            DataTable dt = GetDatatable(query, "client_requests", out fault);

            if (dt.Rows.Count == 1)
            {
                return (bool)dt.Rows[0]["is_requested"];
            }
            else
            {
                return false;
            }
        }

        public static void SetDeviceConnectionDetails(string deviceName, DeviceConnectionStatus status, out bool fault)
        {
            string query = $@" BEGIN;
								INSERT INTO client_connections (client_id, status, connection_timestamp, ssh_ip, ssh_port, ssh_forwarding ) 
                                SELECT clients.id, { (short)status.State }, '{CurrentTimestampString()}', '{status.SshHost}', {status.SshPort}, '{status.SshUser}', {status.SshForwarding}
                                FROM clients where client_name = '{deviceName}'
                                AND NOT EXISTS ( select true from device_requests where client_id = clients.id );

                                UPDATE client_connections SET 
                                    client_id = clients.id, 
                                    status = { (short)status.State },
                                    connection_timestamp = '{CurrentTimestampString()}', 
                                    ssh_ip = '{ status.SshHost}', 
                                    ssh_port = '{ status.SshPort}',
                                    ssh_user = '{ status.SshUser}',
                                    ssh_forwarding = '{status.SshForwarding}'
                                FROM clients where client_name = '{deviceName}'
                                    and client_id = clients.id;
                                COMMIT;
                            ";
            QueryDatabase(query, out fault);
        }

        public static void SetDeviceConnectionState(string deviceName, ClientConnectionState state, out bool fault)
        {
            string query = $@"UPDATE client_connections 
                                SET 
                                        status = {(int)state},
                                        connection_timestamp = '{CurrentTimestampString()}'
                            FROM 
                                clients where clients.client_name = '{deviceName}' and client_connections.client_id = clients.id ;";
            QueryDatabase(query, out fault);
        }

        public static List<int> GetForwardingPorts(out bool fault)
        {
            string query = $@"SELECT ssh_forwarding from client_connections WHERE status != 0";
            DataTable data = GetDatatable(query, "ports", out fault);

            List<int> result = new List<int>();

            if (!fault && data.Rows.Count > 0 )
            {
                foreach (DataRow row in data.Rows)
                {
                    result.Add((int)row["ssh_forwarding"]);
                }
            }

            return result;
        }

        #endregion

        #region developer queries
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

        public static void InsertDeviceConnectionRequest(string deviceName,string developerName, bool isRequest, out bool fault)
        {
            string query =
                            $@" BEGIN;
                                SELECT devices.id, requesters.id ,{ (isRequest ? "true" : "false") }, '{CurrentTimestampString()}'
                                FROM clients as devices
                                CROSS JOIN clients as requesters 
                                WHERE devices.client_name = '{deviceName}' and requesters.client_name = '{developerName}'
                                AND NOT EXISTS ( select true from device_requests where client_id = devices.id );

                                UPDATE device_requests
                                SET is_requested = { (isRequest ? "true" : "false") },
                                	requested_by_client_id = requesters.id,
                                    request_timestamp = '{CurrentTimestampString()}'
                                FROM clients as devices
                                CROSS JOIN clients as requesters 
                                WHERE devices.client_name = '{deviceName}' and requesters.client_name = '{developerName}'
                                and client_id = devices.id;
                                COMMIT;
                            ";

            QueryDatabase(query, out fault);
        }

        #endregion

        #region cyclic checks
        public static void DeactivateOldRequests(int maxRequestAgeInSeconds, out bool fault, out List<string> deactivatedClients)
        {
            string timeLimit = (DateTime.UtcNow - new TimeSpan(0, 0, maxRequestAgeInSeconds)).ToString(Constants.DATETIME_FORMAT_STRING);
            string query = $@"
                    update device_requests
                    set is_requested = false
                    from clients
                    where request_timestamp < '{timeLimit}' and is_requested = true
                    and clients.id = device_requests.requested_by_client_id
                    RETURNING clients.client_name;
                    ";

            var data = GetDatatable(query, "updated_clients", out fault);

            deactivatedClients = new List<string>();

            if (!fault && data.Rows.Count > 0)
            {
                foreach (DataRow row in data.Rows)
                {
                    deactivatedClients.Add((string)row["client_name"]);
                }
            }
        }

        public static void ResetOldConnections(int maxConnectionAgeInSeconds, out bool fault)
        {
            string timeLimit = (DateTime.UtcNow - new TimeSpan(0, 0, maxConnectionAgeInSeconds)).ToString(Constants.DATETIME_FORMAT_STRING);
            string query = $"update client_connections set status = 0 where connection_timestamp < '{timeLimit}' and status != 0;";
            QueryDatabase(query, out fault);
        }

        #endregion

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

        private static object dbLocker = new object();

        private static DataTable GetDatatable(string query, string tablename, out bool fault)
        {
            DataTable result = null;
            lock (dbLocker)
            {
                OpenConnection(ref DbConnection, out fault);

                if (!fault)
                {
                    string commandString = "";

                    commandString = query;

                    result = new DataTable(tablename);

                    GetTableFromDB(DbConnection, ref result, commandString, true);
                }
            }
            
            return result;
        }

        private static void GetTableFromDB(NpgsqlConnection currentConnection, ref DataTable table, string selectString, bool setPrimaryKey)
        {
           
           bool connectionFault = false;

            lock (dbLocker)
            {
                OpenConnection(ref currentConnection, out connectionFault);

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
                    /*
                    if (table.PrimaryKey != null && setPrimaryKey && table.Columns.Count > 0)
                    {
                        table.PrimaryKey =
                            new DataColumn[] { table.Columns[0] };
                    }
                    */
                }
            }
        }

        private static void QueryDatabase(string query, out bool fault)
        {
            lock (dbLocker)
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
        }
   
    }
}