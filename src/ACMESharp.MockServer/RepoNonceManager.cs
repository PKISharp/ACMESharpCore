using System;
using ACMESharp.MockServer.Storage;

namespace ACMESharp.MockServer
{
    public class RepoNonceManager : INonceManager
    {
        private IRepository _repo;

        public RepoNonceManager(IRepository repo)
        {
            _repo = repo;
        }
        public string GenerateNonce()
        {
            var nonce = new DbNonce
            {
                Nonce = Guid.NewGuid().ToString(),
            };
            _repo.SaveNonce(nonce);
            return nonce.Nonce;
        }

        public bool PeekNonce(string nonce)
        {
            var dbNonce = _repo.GetNonceByValue(nonce);
            return dbNonce != null;
        }

        public bool ValidateNonce(string nonce)
        {
            var dbNonce = _repo.GetNonceByValue(nonce);
            if (dbNonce == null)
                return false;
            
            _repo.RemoveNonce(dbNonce);
            return true;
        }
    }
}