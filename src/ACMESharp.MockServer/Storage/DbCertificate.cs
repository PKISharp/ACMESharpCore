namespace ACMESharp.MockServer.Storage
{
    public class DbCertificate
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        public string CertKey { get; set; }

        public string Pem { get; set; }

        public byte[] Native { get; set; }
    }
}