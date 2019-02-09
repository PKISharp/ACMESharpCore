using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.Protocol;
using LiteDB;

namespace ACMEForms.Storage
{
    public class Repository
    {
        public const string DefaultDbName = "acmeforms.db";

        public string DbName { get; private set; }

        public static Repository GetInstance(string dbName = DefaultDbName)
        {
            return new Repository
            {
                DbName = dbName,
            };
        }

        public DbAccount GetAccount()
        {
            using (var db = new LiteDatabase(DbName))
            {
                return db.GetCollection<DbAccount>()
                    .FindAll()
                    .FirstOrDefault();
            }
        }

        public void SaveAccount(DbAccount acct)
        {
            using (var db = new LiteDatabase(DbName))
            {
                db.GetCollection<DbAccount>().Upsert(acct);
            }
        }

        public IEnumerable<DbOrder> GetOrders()
        {
            using (var db = new LiteDatabase(DbName))
            {
                return db.GetCollection<DbOrder>()
                    .FindAll();
            }
        }

        public void Saveorder(DbOrder order)
        {
            using (var db = new LiteDatabase(DbName))
            {
                db.GetCollection<DbOrder>().Upsert(order);
            }
        }
    }
}
