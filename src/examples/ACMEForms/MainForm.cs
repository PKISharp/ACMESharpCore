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

        public MainForm()
        {
            Repo = Program.Repo;
            InitializeComponent();
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

        private (bool valid, DateTime date) ParseDateRangeDate(string value)
        {
            if (!string.IsNullOrEmpty(value) && DateTime.TryParse(value, out var dt))
                return (true, dt);

            return (false, new DateTime(1900, 1, 1));
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

            var authzs = _lastOrder?.Authorizations?.Select(x => x.Details.Identifier.Value).ToArray();
            authorizationsListBox.DataSource = authzs;
            authorizationsListBox.SelectedIndex = authzs?.Length > 0 ? 0 : -1;
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
                    Repo.Saveorder(_lastOrder);

                    RebindOrderControls();
                    SetStatus("Authorization details resolved and saved");
                }
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
                    Repo.Saveorder(_lastOrder);

                    RebindOrderControls();
                    SetStatus("Order details refreshed and saved");
                }

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
                                break;
                            case Http01ChallengeValidationDetails.Http01ChallengeType:
                                authz.HttpChallenge= AuthorizationDecoder.ResolveChallengeForHttp01(
                                    authz.Details, ch, signer);
                                break;
                            default:
                                miscList.Add(ch);
                                break;
                        }
                    }
                    authz.MiscChallenges = miscList.ToArray();
                }
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
            var authz = _lastOrder?.Authorizations?[authorizationsListBox.SelectedIndex];
            identifierTypeTextBox.Text = authz?.Details.Identifier.Type;
            authzUrlTextBox.Text = authz?.Url;
            isWildcardCheckBox.Checked = authz?.Details.Wildcard ?? false;
            authzStatusTextBox.Text = authz?.Details.Status;
            authzExpiresTextBox.Text = authz?.Details.Expires;

            miscChallengeTypesListBox.DataSource =
                    authz.MiscChallenges.Select(x => x.Type) ?? EmptyStrings;

            // Force a refresh of the challenge details
            challengesTabControl_SelectedIndexChanged(sender, e);
        }

        private void challengesTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            var authz = _lastOrder?.Authorizations?[authorizationsListBox.SelectedIndex];
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
            challengeTypeTextBox.Text = ch?.Status;
            challengeTypeTextBox.Text = JsonConvert.SerializeObject(ch?.Error ?? string.Empty);
            challengeTypeTextBox.Text = ch?.Token;
            challengeTypeTextBox.Text = ch?.Url;
            challengeTypeTextBox.Text = ch?.Validated;
            challengeTypeTextBox.Text = JsonConvert.SerializeObject(ch?.ValidationRecord ?? EmptyStrings);
        }
    }
}
