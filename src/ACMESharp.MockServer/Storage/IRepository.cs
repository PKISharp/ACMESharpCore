using System.Collections.Generic;

namespace ACMESharp.MockServer.Storage
{
    public interface IRepository
    {
        void SaveNonce(DbNonce nonce);
        void RemoveNonce(DbNonce nonce);
        DbNonce GetNonce(int id);
        DbNonce GetNonceByValue(string nonce);

        void SaveAccount(DbAccount acct);
        DbAccount GetAccount(int id);
        DbAccount GetAccountByJwk(string jwk);
        DbAccount GetAccountByKid(string kid);

        void SaveOrder(DbOrder order);
        DbOrder GetOrder(int id);
        DbOrder GetOrderByUrl(string url);
        IEnumerable<DbOrder> GetOrdersByAccountId(int id);

        void SaveAuthorization(DbAuthorization authz);
        DbAuthorization GetAuthorization(int id);
        DbAuthorization GetAuthorizationByUrl(string url);
        IEnumerable<DbAuthorization> GetAuthorizationsByOrderId(int id);

        void SaveChallenge(DbChallenge chlng);
        DbChallenge GetChallenge(int id);
        DbChallenge GetChallengeByUrl(string url);
        IEnumerable<DbChallenge> GetChallengesByAuthorizationId(int id);

        void SaveCertificate(DbCertificate cert);
        DbCertificate GetCertificate(int id);
        DbCertificate GetCertificateByKey(string key);
        IEnumerable<DbCertificate> GetCertificatesByOrderId(int id);
    }
}