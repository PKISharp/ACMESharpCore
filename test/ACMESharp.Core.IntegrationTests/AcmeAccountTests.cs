using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using ACMESharp.Testing.Xunit;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace ACMESharp.IntegrationTests
{
    [Collection(nameof(AcmeAccountTests))]
    [CollectionDefinition(nameof(AcmeAccountTests))]
    [TestOrder(0_05)]
    public class AcmeAccountTests : IntegrationTest,
        IClassFixture<StateFixture>,
        IClassFixture<ClientsFixture>
    {
        public AcmeAccountTests(ITestOutputHelper output, StateFixture state, ClientsFixture clients)
            : base(state, clients)
        {
            Output = output;
        }

        // https://xunit.github.io/docs/capturing-output
        ITestOutputHelper Output { get; }

        public static readonly IEnumerable<string> _contactsInit =
                new[] { "mailto:foo@example.com" };
        public static readonly IEnumerable<string> _contactsUpdate =
                new[] { "mailto:bar@example.com", "mailto:baz@example.com" };
        public static readonly IEnumerable<string> _contactsFinal =
                new[] { "mailto:foo@example.com", "mailto:bar@example.com", "mailto:baz@example.com" };


        [Fact]
        [TestOrder(0)]
        public void InitAcmeClient()
        {
            Clients.BaseAddress = new Uri(Constants.LetsEncryptV2StagingEndpoint);
            Clients.Http = new HttpClient()
            {
                BaseAddress = Clients.BaseAddress
            };
            Clients.Acme = new AcmeClient(Clients.Http);
        }

        [Fact]
        [TestOrder(0_010)]
        public async void TestDirectory()
        {
            var tctx = SetTestContext();

            SetTestContext(0);
            var dir = await Clients.Acme.GetDirectoryAsync();

            SetTestContext(1);
            Clients.Acme.Directory = dir;
            await Clients.Acme.GetNonceAsync();

            this.SaveObject("dir.json", dir);
        }

        [Fact]
        [TestOrder(0_020)]
        public async void TestCheckNonExistentAccount()
        {
            var tctx = SetTestContext();

            await Assert.ThrowsAnyAsync<Exception>(
                () => Clients.Acme.CheckAccountAsync());
        }

        [Fact]
        [TestOrder(0_030)]
        public async void TestCreateAccount()
        {
            var tctx = SetTestContext();

            var acct = await Clients.Acme.CreateAccountAsync(_contacts, true);
            this.SaveObject("acct.json", acct);
            Clients.Acme.Account = acct;
        }

        [Fact]
        [TestOrder(0_040)]
        public async void TestCheckNewlyCreatedAccount()
        {
            var tctx = SetTestContext();

            await Clients.Acme.CheckAccountAsync();
        }

        [Fact]
        [TestOrder(0_050)]
        public async void TestDuplicateCreateAccount()
        {
            var tctx = SetTestContext();

            var oldAcct = this.LoadObject<AcmeAccount>("acct.json");
            var dupAcct = await Clients.Acme.CreateAccountAsync(_contacts, true);

            // For a duplicate account, the returned object is not complete...
            Assert.Null(dupAcct.TosLink);
            Assert.Null(dupAcct.PublicKey);
            Assert.Null(dupAcct.Contacts);
            Assert.Null(dupAcct.Id);

            // ...but the KID should be there and identical
            Assert.Equal(oldAcct.Kid, dupAcct.Kid);
        }

        [Fact]
        [TestOrder(0_060)]
        public async void TestDuplicateCreateAccountWithThrow()
        {
            var tctx = SetTestContext();

            await Assert.ThrowsAnyAsync<Exception>(
                () => Clients.Acme.CreateAccountAsync(_contacts, true,
                        throwOnExistingAccount: true));
        }
    }
}
