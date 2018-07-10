using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using ACMESharp.Protocol;
using ACMESharp.Protocol.Resources;
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
        // Will only be displayed if containing test fails.
        ITestOutputHelper Output { get; }

        public static readonly IEnumerable<string> _contactsInit =
                new[] { "mailto:acme-test-foo@mailinator.com" };
        public static readonly IEnumerable<string> _contactsUpdate =
                new[] { "mailto:acme-test-bar@mailinator.com", "mailto:acme-test-baz@mailinator.com" };
        public static readonly IEnumerable<string> _contactsFinal =
                new[] { "mailto:acme-test-foo@mailinator.com", "mailto:acme-test-bar@mailinator.com", "mailto:acme-test-baz@mailinator.com" };


        [Fact]
        [TestOrder(0)]
        public void InitAcmeClient()
        {
            Clients.BaseAddress = new Uri(Constants.LetsEncryptV2StagingEndpoint);
            Clients.Http = new HttpClient()
            {
                BaseAddress = Clients.BaseAddress
            };
            Clients.Acme = new AcmeProtocolClient(Clients.Http, new Crypto.JOSE.JwsTool("EC256"));
        }

        [Fact]
        [TestOrder(0_010)]
        public async Task TestDirectory()
        {
            var testestCtx = SetTestContext();

            SetTestContext(0);
            var dir = await Clients.Acme.GetDirectoryAsync();

            SetTestContext(1);
            Clients.Acme.Directory = dir;
            await Clients.Acme.GetNonceAsync();

            this.SaveObject("dir.json", dir);
        }

        [Fact]
        [TestOrder(0_020)]
        public async Task TestCheckNonExistentAccount()
        {
            var testCtx = SetTestContext();

            await Assert.ThrowsAnyAsync<InvalidOperationException>(
                () => Clients.Acme.CheckAccountAsync());
        }

        [Fact]
        [TestOrder(0_030)]
        public async Task TestCreateAccount()
        {
            var testCtx = SetTestContext();

            var acct = await Clients.Acme.CreateAccountAsync(_contactsInit, true);
            this.SaveObject("acct.json", acct);
            Clients.Acme.Account = acct;
        }

        [Fact]
        [TestOrder(0_040)]
        public async Task TestCheckNewlyCreatedAccount()
        {
            var testCtx = SetTestContext();

            var acct = await Clients.Acme.CheckAccountAsync();
            testCtx.SaveObject("acct-lookup.json", acct);
        }

        [Fact]
        [TestOrder(0_050)]
        public async Task TestDuplicateCreateAccount()
        {
            var testCtx = SetTestContext();

            var oldAcct = this.LoadObject<AcmeAccount>("acct.json");
            var dupAcct = await Clients.Acme.CreateAccountAsync(_contactsInit, true);

            // For a duplicate account, the returned object is not complete...
            Assert.Null(dupAcct.TosLink);

            // ...but the KID should be there and identical
            Assert.Equal(oldAcct.Kid, dupAcct.Kid);
        }

        [Fact]
        [TestOrder(0_060)]
        public async Task TestDuplicateCreateAccountWithThrow()
        {
            var testCtx = SetTestContext();

            await Assert.ThrowsAnyAsync<InvalidOperationException>(
                () => Clients.Acme.CreateAccountAsync(_contactsInit, true,
                        throwOnExistingAccount: true));
        }

        [Fact]
        [TestOrder(0_070)]
        public async Task TestUpdateAccount()
        {
            var testCtx = SetTestContext();

            var acct = await Clients.Acme.UpdateAccountAsync(_contactsUpdate);
            testCtx.SaveObject("acct-updated.json", acct);
        }

        [Fact]
        [TestOrder(0_080)]
        public async Task TestRotateAccountKey()
        {
            var testCtx = SetTestContext();

            var newKey = new Crypto.JOSE.JwsTool("RS384");

            var acct = await Clients.Acme.ChangeAccountKeyAsync(newKey);
            testCtx.SaveObject("acct-keychanged.json", acct);
        }

        [Fact]
        [TestOrder(0_085)]
        public async Task TestUpdateAccountAfterKeyRotation()
        {
            var testCtx = SetTestContext();

            var acct = await Clients.Acme.UpdateAccountAsync(_contactsFinal);
            testCtx.SaveObject("acct-updatednewkey.json", acct);
        }

        [Fact]
        [TestOrder(0_090)]
        public async Task TestDeactivateAccount()
        {
            var testCtx = SetTestContext();

            var acct = await Clients.Acme.DeactivateAccountAsync();
            testCtx.SaveObject("acct-deactivated.json", acct);
        }

        [Fact]
        [TestOrder(0_095)]
        public async Task TestUpdateAccountAfterDeactivation()
        {
            var testCtx = SetTestContext();

            var ex = await Assert.ThrowsAnyAsync<AcmeProtocolException>(
                () => Clients.Acme.UpdateAccountAsync(_contactsUpdate));
            
            Assert.Equal(ProblemType.Unauthorized, ex.ProblemType);
            Assert.Contains("deactivated", ex.ProblemDetail,
                    StringComparison.OrdinalIgnoreCase);
            Assert.Equal((int)HttpStatusCode.Forbidden, ex.ProblemStatus);
        }
    }
}
