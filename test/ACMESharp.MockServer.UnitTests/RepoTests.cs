using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using ACMESharp.Protocol;
using System;
using ACMESharp.Protocol.Resources;
using ACMESharp.MockServer.Storage;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;

namespace ACMESharp.MockServer.UnitTests
{
    [TestClass]
    public class RepoTests
    {
        private static readonly char S = Path.DirectorySeparatorChar;

        public static readonly string DataFolder = $@".{S}_IGNORE{S}data";
        public static readonly string LiteDbFilePath = DataFolder + $@"{S}acme-repo-tests.db";
        static IRepository _repo;

        [ClassInitialize]
        public static void Init(TestContext ctx)
        {
            if (File.Exists(LiteDbFilePath))
                File.Delete(LiteDbFilePath);

            _repo = Storage.Impl.LiteDbRepo.GetInstance(LiteDbFilePath);
        }

        [TestMethod]
        public void SaveAndFindNonce()
        {
            var nonceVal = Guid.NewGuid().ToString();
            var emptyNonceVal = Guid.Empty.ToString();
            var dbNonce = new DbNonce
            {
                Nonce = nonceVal,
            };
            Assert.AreEqual(0, dbNonce.Id);
            _repo.SaveNonce(dbNonce);
            Assert.AreEqual(1, dbNonce.Id);

            // Get Nonce by DB ID
            var getNonce = _repo.GetNonce(dbNonce.Id);
            Assert.IsNotNull(getNonce, "existing nonce by DB ID");
            Assert.AreEqual(dbNonce.Id, getNonce.Id);
            Assert.AreEqual(dbNonce.Nonce, getNonce.Nonce);

            getNonce = _repo.GetNonce(int.MaxValue);
            Assert.IsNull(getNonce, "non-existent nonce by DB ID");

            // Get Nonce by Value
            getNonce = _repo.GetNonceByValue(emptyNonceVal);
            Assert.IsNull(getNonce, "non-existent nonce by value");

            getNonce = _repo.GetNonceByValue(nonceVal);
            Assert.IsNotNull(getNonce, "existing nonce by value");

            // Remove
            _repo.RemoveNonce(getNonce);
            getNonce = _repo.GetNonce(dbNonce.Id);
            Assert.IsNull(getNonce, "existing nonce after remove by DB ID");
            getNonce = _repo.GetNonceByValue(nonceVal);
            Assert.IsNull(getNonce, "existing nonce after remove by value");
        }

        [TestMethod]
        public void SaveAndFindAccount()
        {
            var emptyGuid = Guid.Empty.ToString();
            var key = new Dictionary<string, object>
            {
                ["typ"] = "EC",
                ["crv"] = "P-256",
                ["x"] = 1,
                ["y"] = 2
            };
            var jwk = JsonSerializer.Serialize(key, JsonHelpers.JsonWebOptions);
            var kid = Guid.NewGuid();

            var dbAcct = new DbAccount
            {
                Jwk = jwk,
                Details = new AccountDetails
                {
                    Kid = kid.ToString(),
                    Payload = new Account
                    {
                        Id = BitConverter.ToString(kid.ToByteArray()),
                        Key = key,
                        Contact =
                        [
                            "foo1@bar.com",
                            "foo2@bar.com",
                        ]
                    }
                }
            };

            Assert.AreEqual(0, dbAcct.Id);
            _repo.SaveAccount(dbAcct);
            Assert.AreEqual(1, dbAcct.Id);

            // Get by DB ID
            var getAcct = _repo.GetAccount(dbAcct.Id);
            AssertEqual(dbAcct, getAcct, "existing account by DB ID");

            // Get by JWK
            getAcct = _repo.GetAccountByJwk(jwk);
            AssertEqual(dbAcct, getAcct, "existing account by JWK");
            getAcct = _repo.GetAccountByJwk(kid.ToString());
            Assert.IsNull(getAcct);

            // Get by KID
            getAcct = _repo.GetAccountByKid(kid.ToString());
            AssertEqual(dbAcct, getAcct, "existing account by KID");
            getAcct = _repo.GetAccountByKid(jwk);
            Assert.IsNull(getAcct);

            void AssertEqual(DbAccount exp, DbAccount act,
                    string message = null)
            {
                Assert.IsNotNull(getAcct, message);
                Assert.AreEqual(dbAcct.Id, getAcct.Id, message);
                Assert.AreEqual(dbAcct.Details.Kid, getAcct.Details.Kid, message);
                Assert.AreEqual(dbAcct.Jwk, getAcct.Jwk, message);
                CollectionAssert.AreEqual(
                        (Dictionary<string, object>)dbAcct.Details.Payload.Key,
                        (Dictionary<string, object>)getAcct.Details.Payload.Key, message);
                CollectionAssert.AreEqual(
                        dbAcct.Details.Payload.Contact,
                        getAcct.Details.Payload.Contact, message);
            }
        }
    }

    public class AccountJwkKey
    {

    }
}
