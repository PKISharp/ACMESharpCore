using System.Linq;
using LiteDB;

namespace ACMESharp.MockServer.Storage
{
    public class Repository
    {
        public const string DefaultDbName = "acme-mockserver.db";

        public string DbName { get; private set; }

        public static Repository GetInstance(string dbName = DefaultDbName)
        {
            return new Repository
            {
                DbName = dbName,
            };
        }

        public void SaveNonce(DbNonce nonce)
        {
            using (var db = new LiteDatabase(DbName))
            {
                db.GetCollection<DbNonce>()
                    .EnsureIndex(nameof(DbNonce.Nonce), true);
                db.GetCollection<DbNonce>().Upsert(nonce);
            }
        }

        public void RemoveNonce(DbNonce nonce)
        {
            using (var db = new LiteDatabase(DbName))
            {
                db.GetCollection<DbNonce>().Delete(nonce.Id);
            }
        }

        public DbNonce GetNonce(int id)
        {
            using (var db = new LiteDatabase(DbName))
            {
                return db.GetCollection<DbNonce>()
                    .FindById(id);
            }
        }

        public DbNonce GetNonceByValue(string nonce)
        {
            using (var db = new LiteDatabase(DbName))
            {
                db.GetCollection<DbNonce>()
                    .EnsureIndex(nameof(DbNonce.Nonce), true);
                return db.GetCollection<DbNonce>()
                    .FindOne(x => x.Nonce == nonce);
            }
        }

        public void SaveAccount(DbAccount acct)
        {
            using (var db = new LiteDatabase(DbName))
            {
                db.GetCollection<DbAccount>().Upsert(acct);
            }
        }

        public DbAccount GetAccount(int id)
        {
            using (var db = new LiteDatabase(DbName))
            {
                return db.GetCollection<DbAccount>()
                    .FindById(id);
            }
        }
    }
}