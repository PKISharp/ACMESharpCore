using System.Collections.Generic;
using System.Linq;
using LiteDB;
using Newtonsoft.Json;

namespace ACMESharp.MockServer.Storage.Impl
{
    public class LiteDbRepo : IRepository
    {
        public const string DefaultDbName = "acme-mockserver.db";

        public string DbName { get; private set; }

        public static LiteDbRepo GetInstance(string dbName = DefaultDbName)
        {
            return new LiteDbRepo
            {
                DbName = dbName,
            };
        }

        public void SaveNonce(DbNonce nonce)
        {
            using (var db = new LiteDatabase(DbName))
            {
                db.GetCollection<DbNonce>()
                    .EnsureIndex(x => x.Nonce, true);
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
                return db.GetCollection<DbNonce>()
                    .FindOne(x => x.Nonce == nonce);
            }
        }

        public void SaveAccount(DbAccount acct)
        {
            using (var db = new LiteDatabase(DbName))
            {
                db.GetCollection<DbAccount>()
                    .EnsureIndex(x => x.Jwk, true);
                db.GetCollection<DbAccount>()
                    .EnsureIndex(x => x.Details.Kid, true);
                db.GetCollection<DbAccount>()
                    .Upsert(acct);
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

        public DbAccount GetAccountByJwk(string jwk)
        {
            using (var db = new LiteDatabase(DbName))
            {
                return db.GetCollection<DbAccount>()
                    .FindOne(x => x.Jwk == jwk);
            }

        }

        public DbAccount GetAccountByKid(string kid)
        {
            using (var db = new LiteDatabase(DbName))
            {
                return db.GetCollection<DbAccount>()
                    .FindOne(x => x.Details.Kid == kid);
            }

        }

        public void SaveOrder(DbOrder order)
        {
            using (var db = new LiteDatabase(DbName))
            {
                db.GetCollection<DbOrder>()
                    .EnsureIndex(x => x.Url, true);
                db.GetCollection<DbOrder>()
                    .EnsureIndex(x => x.AccountId);
                db.GetCollection<DbOrder>().Upsert(order);
            }
        }

        public DbOrder GetOrder(int id)
        {
            using (var db = new LiteDatabase(DbName))
            {
                return db.GetCollection<DbOrder>()
                    .FindById(id);
            }
        }

        public DbOrder GetOrderByUrl(string url)
        {
            using (var db = new LiteDatabase(DbName))
            {
                return db.GetCollection<DbOrder>()
                    .FindOne(x => x.Url == url);
            }
        }

        public IEnumerable<DbOrder> GetOrdersByAccountId(int id)
        {
            using (var db = new LiteDatabase(DbName))
            {
                return db.GetCollection<DbOrder>()
                    .Find(x => x.AccountId == id);
            }
        }

        public void SaveAuthorization(DbAuthorization authz)
        {
            using (var db = new LiteDatabase(DbName))
            {
                db.GetCollection<DbAuthorization>()
                    .EnsureIndex(x => x.Url, true);
                db.GetCollection<DbAuthorization>()
                    .EnsureIndex(x => x.OrderId);
                db.GetCollection<DbAuthorization>().Upsert(authz);
            }
        }

        public DbAuthorization GetAuthorization(int id)
        {
            using (var db = new LiteDatabase(DbName))
            {
                return db.GetCollection<DbAuthorization>()
                    .FindById(id);
            }
        }
        public DbAuthorization GetAuthorizationByUrl(string url)
        {
            using (var db = new LiteDatabase(DbName))
            {
                return db.GetCollection<DbAuthorization>()
                    .FindOne(x => x.Url == url);
            }
        }
        public IEnumerable<DbAuthorization> GetAuthorizationsByOrderId(int id)
        {
            using (var db = new LiteDatabase(DbName))
            {
                return db.GetCollection<DbAuthorization>()
                    .Find(x => x.OrderId == id);
            }
        }

        public void SaveChallenge(DbChallenge chlng)
        {
            using (var db = new LiteDatabase(DbName))
            {
                db.GetCollection<DbChallenge>()
                    .EnsureIndex(x => x.Challenge.Url, true);
                db.GetCollection<DbChallenge>()
                    .EnsureIndex(x => x.AuthorizationId);
                db.GetCollection<DbChallenge>().Upsert(chlng);
            }
        }

        public DbChallenge GetChallenge(int id)
        {
            using (var db = new LiteDatabase(DbName))
            {
                return db.GetCollection<DbChallenge>()
                    .FindById(id);
            }
        }
        public DbChallenge GetChallengeByUrl(string url)
        {
            using (var db = new LiteDatabase(DbName))
            {
                return db.GetCollection<DbChallenge>()
                    .FindOne(x => x.Challenge.Url == url);
            }
        }
        public IEnumerable<DbChallenge> GetChallengesByAuthorizationId(int id)
        {
            using (var db = new LiteDatabase(DbName))
            {
                return db.GetCollection<DbChallenge>()
                    .Find(x => x.AuthorizationId == id);
            }
        }
    }
}