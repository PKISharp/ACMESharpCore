using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using LiteDB;

namespace ACMESharp.MockServer.Storage.Impl
{
    public class LiteDbRepo : IRepository
    {
        public const string DefaultDbName = "acme-mockserver.db";

        public string DbName { get; private set; }

        public static LiteDbRepo GetInstance(string dbName = DefaultDbName)
        {
            var repo = new LiteDbRepo
            {
                DbName = dbName,
            };
            repo.Init();
            return repo;
        }

        private void Init()
        {
            using (var db = new LiteDatabase(DbName))
            {
                // DbNonce
                db.GetCollection<DbNonce>()
                    .EnsureIndex(x => x.Nonce, true);
                // DbAccount
                db.GetCollection<DbAccount>()
                    .EnsureIndex(x => x.Jwk, true);
                db.GetCollection<DbAccount>()
                    .EnsureIndex(x => x.Details.Kid, true);
                // DbOrder
                db.GetCollection<DbOrder>()
                    .EnsureIndex(x => x.Url, true);
                db.GetCollection<DbOrder>()
                    .EnsureIndex(x => x.AccountId);
                // DbAuthorization
                db.GetCollection<DbAuthorization>()
                    .EnsureIndex(x => x.Url, true);
                db.GetCollection<DbAuthorization>()
                    .EnsureIndex(x => x.OrderId);
                // DbChallenge
                db.GetCollection<DbChallenge>()
                    .EnsureIndex(x => x.Payload.Url, true);
                db.GetCollection<DbChallenge>()
                    .EnsureIndex(x => x.AuthorizationId);
                // DbCertificate
                db.GetCollection<DbCertificate>()
                    .EnsureIndex(x => x.CertKey, true);
                db.GetCollection<DbCertificate>()
                    .EnsureIndex(x => x.Thumbprint, true);
                db.GetCollection<DbCertificate>()
                    .EnsureIndex(x => x.OrderId);
            }
        }

        public void SaveNonce(DbNonce nonce)
        {
            using (var db = new LiteDatabase(DbName))
            {
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
                    .FindOne(x => x.Payload.Url == url);
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

        public void SaveCertificate(DbCertificate cert)
        {
            using (var db = new LiteDatabase(DbName))
            {
                db.GetCollection<DbCertificate>().Upsert(cert);
            }
        }

        public DbCertificate GetCertificate(int id)
        {
            using (var db = new LiteDatabase(DbName))
            {
                return db.GetCollection<DbCertificate>()
                    .FindById(id);
            }
        }
        public DbCertificate GetCertificateByKey(string certKey)
        {
            using (var db = new LiteDatabase(DbName))
            {
                return db.GetCollection<DbCertificate>()
                    .FindOne(x => x.CertKey == certKey);
            }
        }
        public DbCertificate GetCertificateByThumbprint(string thumbprint)
        {
            using (var db = new LiteDatabase(DbName))
            {
                return db.GetCollection<DbCertificate>()
                    .FindOne(x => x.Thumbprint == thumbprint);
            }
        }
        public DbCertificate GetCertificateByNative(byte[] certDer)
        {
            using (var db = new LiteDatabase(DbName))
            {
                var xcrt = new X509Certificate2(certDer);
                return db.GetCollection<DbCertificate>()
                    .FindOne(x => x.Thumbprint == xcrt.Thumbprint);
            }
        }
        public IEnumerable<DbCertificate> GetCertificatesByOrderId(int id)
        {
            using (var db = new LiteDatabase(DbName))
            {
                return db.GetCollection<DbCertificate>()
                    .Find(x => x.OrderId == id);
            }
        }
    }
}