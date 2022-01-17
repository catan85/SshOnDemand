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
    using ApiServer.Entities;
    using ApiServer.Models;

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

        public static DeviceConnectionStatus CheckDeviceConnection(sshondemandContext dbContext, string deviceName)
        {
            DeviceConnectionStatus status = new DeviceConnectionStatus();
            status.State = ClientConnectionState.Disconnected;
            var connectionStatus = dbContext.ClientConnections.Where(c => c.Client.ClientName == deviceName).SingleOrDefault();
            if (connectionStatus != null)
            {
                status.State = (ClientConnectionState)(connectionStatus.Status);
                status.SshHost = connectionStatus.SshIp;
                status.SshPort = connectionStatus.SshPort.Value;
                status.SshForwarding = connectionStatus.SshForwarding.Value;
                status.SshUser = connectionStatus.SshUser;
            }
            return status;
        }
        #endregion

        #region device queries


        public static bool IsDeviceConnectionRequested(sshondemandContext dbContext, string deviceName)
        {
            var request = dbContext.DeviceRequests.SingleOrDefault(r => r.Client.ClientName == deviceName);

            if (request != null)
            {
                return request.IsRequested.Value;
            }
            else
            {
                return false;
            }
        }

        public static void SetDeviceConnectionDetails(string deviceName, DeviceConnectionStatus status, out bool fault)
        {
            string query = $@" BEGIN;
								INSERT INTO client_connections (client_id, status, connection_timestamp, ssh_ip, ssh_port, ssh_user, ssh_forwarding )
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
        public static bool IsDeveloperConnectionToDeviceAuthorized(sshondemandContext dbContext, string developerName, string deviceName)
        {
            return dbContext.DeveloperAuthorizations
                .Any(c => c.Developer.ClientName == developerName && c.Device.ClientName == deviceName);
        }

        public static void InsertDeviceConnectionRequest(sshondemandContext dbContext, string deviceName, string developerName)
        {
            var result = dbContext.DeviceRequests
                .SingleOrDefault(connectionRequest => 
                    connectionRequest.Client.ClientName == deviceName &&
                    connectionRequest.RequestedByClient.ClientName == developerName &&
                    !dbContext.DeviceRequests.Any(existingRequest => existingRequest.ClientId == connectionRequest.ClientId)
                    );

            if (result != null)
            {
                result.IsRequested = true;
                dbContext.SaveChanges();
            }
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

        public static void ResetOldConnections(int maxConnectionAgeInSeconds, out bool fault, out List<string> deactivatedClients)
        {
            string timeLimit = (DateTime.UtcNow - new TimeSpan(0, 0, maxConnectionAgeInSeconds)).ToString(Constants.DATETIME_FORMAT_STRING);
            string query = $@"                               
                            update client_connections set status = 0 
                            FROM clients 
                            where connection_timestamp < '{timeLimit}' and status != 0 
                            and clients.id = client_connections.client_id RETURNING clients.client_name;";

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