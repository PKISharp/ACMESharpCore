using System;
using System.Collections.Generic;
using System.Text;
using ACMESharp.Crypto.JOSE;

namespace Examples.Common.PKI
{
    public class ExamplesAccountKey
    {
        public string KeyType { get; set; }
        public string KeyExport { get; set; }

        public IJwsTool GenerateTool()
        {
            if (KeyType.StartsWith("ES"))
            {
                var tool = new ACMESharp.Crypto.JOSE.Impl.ESJwsTool();
                tool.HashSize = int.Parse(KeyType.Substring(2));
                tool.Init();
                tool.Import(KeyExport);
                return tool;
            }

            if (KeyType.StartsWith("RS"))
            {
                var tool = new ACMESharp.Crypto.JOSE.Impl.RSJwsTool();
                tool.HashSize = int.Parse(KeyType.Substring(2));
                tool.Init();
                tool.Import(KeyExport);
                return tool;
            }

            throw new Exception($"Unknown or unsupported KeyType [{KeyType}]");
        }
    }
}
