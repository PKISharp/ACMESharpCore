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
using ACMESharp.Protocol;
using Examples.Common.PKI;

namespace ACMEForms
{
    public partial class MainForm : Form
    {
        static readonly char[] ValueSeps = "\r\n\t ;".ToCharArray();
        static readonly string[] EmptyStrings = new string[0];

        DbAccount _account;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            caServerComboBox.DataSource = DbAccount.WellKnownAcmeServers.ToList();

            _account = Program.Repo.GetAccount();
            AccountChanged();
       }

        private void AccountChanged()
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

        private void SetStatus(string message, bool addTime = true)
        {
            if (addTime)
                mainStatusLabel.Text = $"{message} at {DateTime.Now}";
            else
                mainStatusLabel.Text = message;
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
                    Repository.GetInstance().SaveAccount(acct);
                    _account = acct;
                }
                AccountChanged();
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
                    Repository.GetInstance().SaveAccount(_account);
                }
                AccountChanged();
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
                    Repository.GetInstance().SaveAccount(_account);
                }
                AccountChanged();
                SetStatus("Account updated and saved");
            });
        }
    }
}
