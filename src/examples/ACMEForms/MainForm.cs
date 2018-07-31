using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ACMEForms.Storage;
using ACMEForms.Util;
using ACMESharp.Authorizations;
using ACMESharp.Protocol;
using ACMESharp.Protocol.Resources;
using Examples.Common.PKI;
using Newtonsoft.Json;

namespace ACMEForms
{
    public partial class MainForm : Form
    {
        static readonly char[] ValueSeps = "\r\n\t ;".ToCharArray();
        static readonly string[] EmptyStrings = new string[0];

        private Repository Repo;

        DbAccount _account;
        DbOrder _lastOrder;

        IList<string> _authzListWrapper;
        IList<string> _miscChallengesListWrapper;

        public MainForm()
        {
            Repo = Program.Repo;

            _authzListWrapper = new ReadOnlyListWrapper<string>(
                    () => _lastOrder?.Authorizations?.Length ?? 0,
                    (i) => _lastOrder?.Authorizations?[i].Details.Identifier.Value);
            _miscChallengesListWrapper = new ReadOnlyListWrapper<string>(
                    () => SelectedAuthorization?.MiscChallenges?.Length ?? 0,
                    (i) => SelectedAuthorization?.MiscChallenges?[i].Type);

            InitializeComponent();

            authorizationsListBox.DataSource = _authzListWrapper;
            miscChallengeTypesListBox.DataSource = _miscChallengesListWrapper;

            accountPropertyGrid.SelectedObject = new AccountDetailsViewModel
            {
                AccountGetter = () => _account,
            };
            orderPropertyGrid.SelectedObject = new OrderDetailsViewModel
            {
                OrderGetter = () => _lastOrder,
            };
        }

        private DbAuthz SelectedAuthorization
        {
            get
            {
                if (authorizationsListBox.SelectedIndex < 0)
                    return null;
                return _lastOrder?.Authorizations?[authorizationsListBox.SelectedIndex];
            }
        }

        private void SetStatus(string message, bool addTime = true)
        {
            if (addTime)
                mainStatusLabel.Text = $"{message} at {DateTime.Now}";
            else
                mainStatusLabel.Text = message;
        }

        private async Task InvokeWithWaitCursor(Func<Task> action)
        {
            var origCursor = this.Cursor;
            try
            {
                this.Cursor = Cursors.AppStarting;

                await action();
            }
            finally
            {
                this.Cursor = origCursor;
            }
        }

        private (bool valid, DateTime date) ParseDateRangeDate(string value)
        {
            if (!string.IsNullOrEmpty(value) && DateTime.TryParse(value, out var dt))
                return (true, dt);

            return (false, new DateTime(1900, 1, 1));
        }

        private string Stringify(object o, object ifNull = null)
        {
            if (o == null)
                if (ifNull is string ifNullString)
                    return ifNullString;
                else
                    o = ifNull;

            return JsonConvert.SerializeObject(o, Formatting.Indented);
        }

        private Uri ResolveCaServerEndpoint()
        {
            var url = caServerComboBox.SelectedValue as string;
            if (string.IsNullOrEmpty(url))
                url = caServerTextBox.Text;
            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("You must select or provide a CA Server endpoint.", "CA Server",
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return null;
            }
            return new Uri(url);
        }

        private IEnumerable<string> ResolveEmailContacts()
        {
            var emails = contactEmailsTextBox.Text.Split(ValueSeps,
                    StringSplitOptions.RemoveEmptyEntries);
            if (emails?.Length == 0)
            {
                MessageBox.Show("You must specify at least one contact email.", "Contact Emails",
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return null;
            }
            return emails.Select(x => $"mailto:{x}").ToArray();
        }

        private IEnumerable<string> ResolveDnsIdentifiers()
        {
            var ids = dnsIdentifiersTextBox.Text.Split(ValueSeps,
                    StringSplitOptions.RemoveEmptyEntries);
            if (ids?.Length == 0)
            {
                MessageBox.Show("You must specify at least one DNS name.", "DNS Identifiers",
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return null;
            }
            return ids;
        }

        private (DateTime? notBefore, DateTime? notAfter)? ResolveOrderDateRange()
        {
            DateTime? notBefore = notBeforeDateTimePicker.Value;
            DateTime? notAfter = notAfterDateTimePicker.Value;
            return (notBeforeDateTimePicker.Checked ? notBefore : null,
                    notAfterDateTimePicker.Checked ? notAfter : null);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            caServerComboBox.DataSource = DbAccount.WellKnownAcmeServers.ToList();

            _account = Repo.GetAccount();
            RebindAccountControls();

            _lastOrder = Repo.GetOrders().LastOrDefault();
            RebindOrderControls();
        }

        private void RebindAccountControls()
        {
            var hasAccount = _account != null;
            var caServerEndpoint = _account?.AcmeServerEndpoint ?? DbAccount.WellKnownAcmeServers.First().Key;
            caServerComboBox.SelectedValue = caServerEndpoint;
            if (caServerComboBox.SelectedIndex == -1)
            {
                caServerComboBox.SelectedValue = string.Empty;
                caServerTextBox.Text = caServerEndpoint;
            }

            caServerComboBox.Enabled = !hasAccount;
            contactEmailsTextBox.Text = string.Join("\r\n",
                    _account?.Details.Payload?.Contact.Select(x => x.Replace("mailto:", ""))
                    ?? EmptyStrings);
            //agreeTosCheckbox.Checked = _account?.Details.Payload.TermsOfServiceAgreed ?? false;
            agreeTosCheckbox.Checked = !string.IsNullOrEmpty(_account?.Details?.TosLink);
            agreeTosCheckbox.Enabled = !hasAccount;

            createAccountButton.Enabled = !hasAccount;
            refreshAccountButton.Enabled = hasAccount;
            //updateAccountButton.Enabled = hasAccount;
            accountDetailsGroupBox.Visible = hasAccount;

            kidTextBox.Text = _account?.Details.Kid;
            tosLinkTextBox.Text = _account?.Details.TosLink;
            statusTextBox.Text = _account?.Details.Payload.Status;
            ordersTextBox.Text = _account?.Details.Payload.Orders;
            initialIpTextBox.Text = _account?.Details.Payload.InitialIp;
            createdAtTextBox.Text = _account?.Details.Payload.CreatedAt;
            agreementTextBox.Text = _account?.Details.Payload.Agreement;


        }

        private void RebindOrderControls()
        {
            var hasOrder = _lastOrder != null;

            createOrderButton.Enabled = !hasOrder;
            refreshOrderButton.Enabled = hasOrder;

            dnsIdentifiersTextBox.ReadOnly = hasOrder;
            dnsIdentifiersTextBox.Text = string.Join("\r\n",
                    _lastOrder?.Details.Payload.Identifiers.Select(x => x.Value)
                    ?? EmptyStrings);

            var notBefore = ParseDateRangeDate(_lastOrder?.Details.Payload.NotBefore);
            notBeforeDateTimePicker.Enabled = !hasOrder;
            notBeforeDateTimePicker.Checked = notBefore.valid;
            notBeforeDateTimePicker.Value = notBefore.date;

            var notAfter = ParseDateRangeDate(_lastOrder?.Details.Payload.NotAfter);
            notAfterDateTimePicker.Enabled = !hasOrder;
            notAfterDateTimePicker.Checked = notAfter.valid;
            notAfterDateTimePicker.Value = notAfter.date;

            orderUrlTextBox.Text = _lastOrder?.Details.OrderUrl;
            firstOrderUrlTextBox.Text = _lastOrder?.FirstOrderUrl;
            orderStatusTextBox.Text = _lastOrder?.Details.Payload.Status;
            orderExpiresTextBox.Text = _lastOrder?.Details.Payload.Expires;

            finalizeUrlTextBox.Text = _lastOrder?.Details.Payload.Finalize;
            certificateUrlTextBox.Text = _lastOrder?.Details.Payload.Certificate;

            errorStatusTextBox.Text = _lastOrder?.Details.Payload.Error?.Status?.ToString();
            errorTypeTextBox.Text = _lastOrder?.Details.Payload.Error?.Type;
            errorDetailTextBox.Text = _lastOrder?.Details.Payload.Error?.Detail;

            //var authzs = _lastOrder?.Authorizations?.Select(x => x.Details.Identifier.Value).ToArray();
            // We have to "re-set" the DS in order for the control to refresh the contents and bounds
            authorizationsListBox.DataSource = null;
            authorizationsListBox.DataSource = _authzListWrapper;
            authorizationsListBox.SelectedIndex = _authzListWrapper.Count > 0 ? 0 : -1;
        }

        public class OrderDetailsViewModel
        {
            // We use strings with increasing number of space characters to define
            // a sort order without actually providing any real category labels
            const string GeneralCat = " ";//"1 - General";
            const string EndpointUrlsCat = "  "; //"2 - Endpoint URLs";
            const string ErrorCat = "   "; //"3 - Error";

            private ProblemViewModel _Error;

            public OrderDetailsViewModel()
            {
                _Error = new ProblemViewModel
                {
                    ProblemGetter = () => OrderGetter()?.Details.Payload.Error,
                };
            }

            [Browsable(false)]
            public Func<DbOrder> OrderGetter { get; set; } = () => null;

            [Category(GeneralCat)]
            public string OrderUrl => OrderGetter()?.Details.OrderUrl;
            [Category(GeneralCat)]
            public string FirstOrderUrl => OrderGetter()?.FirstOrderUrl;
            [Category(GeneralCat)]
            public string Status => OrderGetter()?.Details.Payload.Status;
            [Category(GeneralCat)]
            public string Expires => OrderGetter()?.Details.Payload.Expires;

            [Category(EndpointUrlsCat)]
            public string FinalizeUrl => OrderGetter()?.Details.Payload.Finalize;
            [Category(EndpointUrlsCat)]
            public string CertificateUrl => OrderGetter()?.Details.Payload.Certificate;

            [Category(ErrorCat)]
            public ProblemViewModel Error => OrderGetter()?.Details.Payload.Error == null
                    ? null : _Error;
        }

        [TypeConverter(typeof(ExpandableObjectConverter))]
        public class ProblemViewModel
        {
            [Browsable(false)]
            public Func<Problem> ProblemGetter { get; set; } = () => null;

            public string Type => ProblemGetter()?.Type;

            public int? Status => ProblemGetter()?.Status;

            public string Detail => ProblemGetter()?.Detail;

            public override string ToString()
            {
                return ProblemGetter() == null ? "" : $"({Type}, {Status}, {Detail})";
            }
        }

        [TypeConverter(typeof(ExpandableObjectConverter))]
        public class NestedObjectProperty<T>
        {
            [Browsable(false)]
            public Func<NestedObjectProperty<T>, T> Getter { get; set; }

            [Browsable(false)]
            public Func<NestedObjectProperty<T>, bool> Exister { get; set; } = p => p.Getter(p) != null;
        }

        private void RebindAuthorizationControls()
        {
            var authz = SelectedAuthorization;
            identifierTypeTextBox.Text = authz?.Details.Identifier.Type;
            authzUrlTextBox.Text = authz?.Url;
            isWildcardCheckBox.Checked = authz?.Details.Wildcard ?? false;
            authzStatusTextBox.Text = authz?.Details.Status;
            authzExpiresTextBox.Text = authz?.Details.Expires;

            //miscChallengeTypesListBox.DataSource =
            //        (authz.MiscChallenges?.Select(x => x.Type) ?? EmptyStrings).ToArray();
            // We have to "re-set" the DS in order for the control to refresh the contents and bounds
            miscChallengeTypesListBox.DataSource = null;
            miscChallengeTypesListBox.DataSource = _miscChallengesListWrapper;
            miscChallengeTypesListBox.SelectedIndex = _miscChallengesListWrapper.Count > 0 ? 0 : -1;

            RebindChallengeControls();
        }

        private void RebindChallengeControls()
        {
            var authz = SelectedAuthorization;
            var tp = challengesTabControl.SelectedTab;

            dnsRecordNameTextBox.Text = authz?.DnsChallenge?.DnsRecordName;
            dnsRecordTypeTextBox.Text = authz?.DnsChallenge?.DnsRecordType;
            dnsRecordValueTextBox.Text = authz?.DnsChallenge?.DnsRecordValue;

            httpResourceUrlTextBox.Text = authz?.HttpChallenge?.HttpResourceUrl;
            httpResourcePathTextBox.Text = authz?.HttpChallenge?.HttpResourcePath;
            httpResourceContentTypeTextBox.Text = authz?.HttpChallenge?.HttpResourceContentType;
            httpResourceValueTextBox.Text = authz?.HttpChallenge?.HttpResourceValue;

            Challenge ch = null;

            if (tp == dnsChallengeTabPage)
            {
                ch = authz?.Details.Challenges.FirstOrDefault(
                        x => x.Type == authz?.DnsChallenge?.ChallengeType);
            }
            else if (tp == httpChallengeTabPage)
            {
                ch = authz?.Details.Challenges.FirstOrDefault(
                        x => x.Type == authz?.HttpChallenge?.ChallengeType);
            }
            else if (miscChallengeTypesListBox.SelectedIndex >= 0)
            {
                ch = authz?.Details.Challenges.FirstOrDefault(
                        x => x.Type == (string)miscChallengeTypesListBox.SelectedValue);
            }

            challengeTypeTextBox.Text = ch?.Type;
            challengeStatusTextBox.Text = ch?.Status;
            challengeErrorTextBox.Text = Stringify(ch?.Error, string.Empty);
            challengeTokenTextBox.Text = ch?.Token;
            challengeUrlTextBox.Text = ch?.Url;
            challengeValidatedTextBox.Text = ch?.Validated;
            validationRecordsTextBox.Text = Stringify(ch?.ValidationRecord, string.Empty);
        }

        private void caServerComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            caServerTextBox.Visible = (caServerComboBox.SelectedValue as string) == string.Empty;
        }

        private async void agreeTosLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var url = ResolveCaServerEndpoint();
            if (url == null)
                return;

            var signer = new PkiJwsTool(256);

            using (var acme = new AcmeProtocolClient(url, signer: signer))
            {
                var dir = await acme.GetDirectoryAsync();

                if (string.IsNullOrEmpty(dir.Meta.TermsOfService))
                {
                    MessageBox.Show("CA Server directory meta data contains no ToS link.", "Missing ToS",
                            MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
                System.Diagnostics.Process.Start(dir.Meta.TermsOfService);
            }
        }

        private async void createAccountButton_Click(object sender, EventArgs e)
        {
            if (!agreeTosCheckbox.Checked)
            {
                MessageBox.Show("You must agree to the Terms of Service.", "Terms of Service",
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            var url = ResolveCaServerEndpoint();
            if (url == null)
                return;

            var contacts = ResolveEmailContacts();
            if (contacts == null)
                return;

            await InvokeWithWaitCursor(async () =>
            {
                var signer = new PkiJwsTool(256);
                signer.Init();

                using (var acme = new AcmeProtocolClient(url, signer: signer))
                {
                    var dir = await acme.GetDirectoryAsync();
                    acme.Directory = dir;

                    await acme.GetNonceAsync();

                    var details = await acme.CreateAccountAsync(
                            contacts,
                            agreeTosCheckbox.Checked,
                            throwOnExistingAccount: true);

                    var acct = new DbAccount
                    {
                        AcmeServerEndpoint = url.ToString(),
                        JwsSigner = signer.Export(),
                        Details = details,
                    };
                    Repo.SaveAccount(acct);
                    _account = acct;
                }
                RebindAccountControls();
                SetStatus("Account created and saved");
            });
        }

        private async void refreshAccountButton_Click(object sender, EventArgs e)
        {
            var url = ResolveCaServerEndpoint();
            if (url == null)
                return;

            await InvokeWithWaitCursor(async () =>
            {
                var signer = new PkiJwsTool(256);
                signer.Import(_account.JwsSigner);

                using (var acme = new AcmeProtocolClient(url, signer: signer,
                        acct: _account.Details))
                {
                    var dir = await acme.GetDirectoryAsync();
                    acme.Directory = dir;

                    await acme.GetNonceAsync();

                    var details = await acme.UpdateAccountAsync();
                    _account.Details = details;
                    Repo.SaveAccount(_account);
                }
                RebindAccountControls();
                SetStatus("Account refreshed and saved");
            });
        }

        private async void updateAccountButton_Click(object sender, EventArgs e)
        {
            var url = ResolveCaServerEndpoint();
            if (url == null)
                return;

            var contacts = ResolveEmailContacts();
            if (contacts == null)
                return;

            await InvokeWithWaitCursor(async () =>
            {
                var signer = new PkiJwsTool(256);
                signer.Import(_account.JwsSigner);
                
                using (var acme = new AcmeProtocolClient(url, signer: signer,
                        acct: _account.Details))
                {
                    var dir = await acme.GetDirectoryAsync();
                    acme.Directory = dir;

                    await acme.GetNonceAsync();

                    var details = await acme.UpdateAccountAsync(contacts);
                    _account.Details = details;
                    Repo.SaveAccount(_account);
                }
                RebindAccountControls();
                SetStatus("Account updated and saved");
            });
        }

        private async Task RefreshOrderAuthorizations(AcmeProtocolClient acme)
        {
            var details = _lastOrder.Details;
            var authzs = new List<DbAuthz>();
            foreach (var authzUrl in details.Payload.Authorizations)
            {
                var authzDetails = await acme.GetAuthorizationDetailsAsync(authzUrl);
                authzs.Add(new DbAuthz
                {
                    Url = authzUrl,
                    Details = authzDetails,
                });
            }
            _lastOrder.Authorizations = authzs.ToArray();
        }

        private Task DecodeOrderAuthorizationChallenges(ACMESharp.Crypto.JOSE.IJwsTool signer)
        {
            foreach (var authz in _lastOrder.Authorizations)
            {
                var miscList = new List<Challenge>();
                foreach (var ch in authz.Details.Challenges)
                {
                    switch (ch.Type)
                    {
                        case Dns01ChallengeValidationDetails.Dns01ChallengeType:
                            authz.DnsChallenge = AuthorizationDecoder.ResolveChallengeForDns01(
                                authz.Details, ch, signer);
                            miscList.Add(ch);
                            break;
                        case Http01ChallengeValidationDetails.Http01ChallengeType:
                            authz.HttpChallenge = AuthorizationDecoder.ResolveChallengeForHttp01(
                                authz.Details, ch, signer);
                            miscList.Add(ch);
                            break;
                        default:
                            miscList.Add(ch);
                            break;
                    }
                }
                authz.MiscChallenges = miscList.ToArray();
            }

            return Task.CompletedTask;
        }

        private async void createOrderButton_Click(object sender, EventArgs e)
        {
            var url = ResolveCaServerEndpoint();
            if (url == null)
                return;

            var ids = ResolveDnsIdentifiers();
            if (ids == null)
                return;

            var dateRange = ResolveOrderDateRange();
            if (dateRange == null)
                return;

            await InvokeWithWaitCursor(async () =>
            {
                var signer = new PkiJwsTool(256);
                signer.Import(_account.JwsSigner);

                using (var acme = new AcmeProtocolClient(url, signer: signer,
                        acct: _account.Details))
                {
                    var dir = await acme.GetDirectoryAsync();
                    acme.Directory = dir;

                    await acme.GetNonceAsync();

                    var details = await acme.CreateOrderAsync(ids,
                            dateRange.Value.notBefore, dateRange.Value.notAfter);

                    var order = new DbOrder
                    {
                        FirstOrderUrl = details.OrderUrl,
                        Details = details,
                    };
                    Repo.Saveorder(order);
                    _lastOrder = order;

                    RebindOrderControls();
                    SetStatus("Order created and saved");

                    RefreshOrderAuthorizations(acme);
                }

                Repo.Saveorder(_lastOrder);
                RebindOrderControls();
                SetStatus("Order created and Authorization resolved and saved");

                DecodeOrderAuthorizationChallenges(signer);

                Repo.Saveorder(_lastOrder);
                RebindOrderControls();
                SetStatus("Order created, Authorizations resolved and Challenges decoded and saved");
            });
        }

        private async void refreshOrderButton_Click(object sender, EventArgs e)
        {
            var url = ResolveCaServerEndpoint();
            if (url == null)
                return;

            await InvokeWithWaitCursor(async () =>
            {
                var signer = new PkiJwsTool(256);
                signer.Import(_account.JwsSigner);

                using (var acme = new AcmeProtocolClient(url, signer: signer,
                        acct: _account.Details))
                {
                    var dir = await acme.GetDirectoryAsync();
                    acme.Directory = dir;

                    await acme.GetNonceAsync();

                    var details = await acme.GetOrderDetailsAsync(
                            _lastOrder.Details.OrderUrl ?? _lastOrder.FirstOrderUrl);
                    _lastOrder.Details = details;
                    Repo.Saveorder(_lastOrder);

                    RebindOrderControls();
                    SetStatus("Order refreshed and saved");

                    await RefreshOrderAuthorizations(acme);
                }

                Repo.Saveorder(_lastOrder);
                RebindOrderControls();
                SetStatus("Order details and Authorizations refreshed and saved");

                await DecodeOrderAuthorizationChallenges(signer);

                Repo.Saveorder(_lastOrder);
                RebindOrderControls();
                SetStatus("Order details and Authorizations refreshed, Challenges decoded and saved");
            });
        }

        private void clearOrderButton_Click(object sender, EventArgs e)
        {
            _lastOrder = null;
            RebindOrderControls();
            SetStatus("Last Order has been cleared");
        }

        private void authorizationsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            RebindAuthorizationControls();
        }

        private void challengesTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            RebindChallengeControls();
        }

        private void miscChallengeTypesListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            RebindChallengeControls();
        }
    }


    public class AccountDetailsViewModel
    {
        [Browsable(false)]
        public Func<DbAccount> AccountGetter { get; set; } = () => null;

        [DisplayName("KID")]
        [Description("Key Identifier which uniquely identifies this account with the ACME CA server.")]
        public string KId => AccountGetter()?.Details.Kid;

        [DisplayName("ToS Link")]
        public string TosLink => AccountGetter()?.Details.TosLink;

        public string Status => AccountGetter()?.Details.Payload.Status;

        public string Orders => AccountGetter()?.Details.Payload.Orders;

        [DisplayName("Initial IP")]
        public string InitialIp => AccountGetter()?.Details.Payload.InitialIp;

        [DisplayName("Created At")]
        public string CreatedAt => AccountGetter()?.Details.Payload.CreatedAt;

        public string Agreement => AccountGetter()?.Details.Payload.Agreement;
    }
}
