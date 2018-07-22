namespace ACMESharp.MockServer
{
    public interface INonceManager
    {
        string GenerateNonce();

        bool ValidateNonce(string nonce);

        bool PeekNonce(string nonce);
    }
}