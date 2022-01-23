using ApiServer.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiServer.Infrastructure.Repositories
{
    public class BaseRepository<T> where T : class
    {
        protected readonly sshondemandContext dbContext;

        public BaseRepository(sshondemandContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public T Get(object primaryKey)
        {
            return this.dbContext.Set<T>().Find(primaryKey);
        }

        public bool Add(T newValue)
        {
            dbContext.Add(newValue);
            dbContext.SaveChanges();
            return true;
        }

        public bool Delete(T valueToRemove)
        {
            dbContext.Remove(valueToRemove);
            dbContext.SaveChanges();
            return true;
        }

        public IEnumerable<T> GetAll()
        {
            return dbContext.Set<T>().ToList();
        }

    }
}
