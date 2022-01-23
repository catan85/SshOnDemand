using ApiServer.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiServer.Infrastructure.Repositories
{
    public class ClientConnectionsRepository : BaseRepository<ClientConnection>
    {
        public ClientConnectionsRepository(sshondemandContext dbContext) : base(dbContext) { }

        public ClientConnection GetByClientName(string clientName)
        {
            return this.dbContext.ClientConnections.SingleOrDefault(c => c.Client.ClientName == clientName);
        }
    }
}
