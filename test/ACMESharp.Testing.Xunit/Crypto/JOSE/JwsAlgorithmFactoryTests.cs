using System;
using System.Collections.Generic;
using ACMESharp.Crypto.JOSE;
using Xunit;

namespace ACMESharp.UnitTests.Crypto.JOSE
{
    public class JwsAlgorithmFactoryTests
    {
        [Theory, MemberData(nameof(AlgoNames))]
        public void AlgorithmWillBeCreated(string algoName)
        {
            var sut = new JwsAlgorithmFactory();

            var algo = sut.Create(algoName);
            Assert.NotNull(algo);
        }

        public static IEnumerable<object[]> AlgoNames()
        {
            yield return new object[] { "RS256-2048" };
        }
    }
}
