using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.Protocol;
using Examples.Common.PKI;
using Newtonsoft.Json;
using PKISharp.SimplePKI;
using Terminal.Gui;

using static System.Console;

namespace ACMETerm
{
    public class Program
    {
        public const string LetsEncryptV2StagingEndpoint = "https://acme-staging-v02.api.letsencrypt.org/";

        public const string LetsEncryptV2Endpoint = "https://acme-v02.api.letsencrypt.org/";

        private HttpClient _http = new HttpClient();
        private string _contacts = "";
        private bool _agreeTos = true;
        private string _dnsNames = "";

        static async Task Main(string[] args)
        { 
            var p = new Program();
            
            await p.GetTermsOfService();
            await p.CreateAccount();
        }

        async Task GetTermsOfService()
        {
            var signer = new ACMESharp.Crypto.JOSE.Impl.RSJwsTool();
            var acmeUrl = new Uri(Program.LetsEncryptV2StagingEndpoint);
            _http = new HttpClient();
            _http.BaseAddress = acmeUrl;

            WriteLine("getting from: " + acmeUrl);

            using (var acme = new AcmeProtocolClient(_http, signer: signer))
            {
                acme.BeforeAcmeSign = (s,o) => WriteLine($"BEFORE({s}, {JsonConvert.SerializeObject(o)})");

                var dir = await acme.GetDirectoryAsync();
                WriteLine("Got Directory: " + dir);
                WriteLine("TOS: " + dir.Meta?.TermsOfService);
            }
        }

        async Task CreateAccount()
        {
            var acmeUrl = new Uri(Program.LetsEncryptV2StagingEndpoint);
            _http = new HttpClient();
            _http.BaseAddress = acmeUrl;

            var sample = Encoding.UTF8.GetBytes("abcdefg");

            // var ecKeys = PkiKeyPair.GenerateEcdsaKeyPair(256);
            // var ecPrvKey = ecKeys.PrivateKey.Export(PkiEncodingFormat.Pem);
            // var ecPubKey = ecKeys.PublicKey.Export(PkiEncodingFormat.Pem);

            // WriteLine(
            //     "getting from: " + acmeUrl
            //     + "\r\necPrv: " + Convert.ToBase64String(ecPrvKey)
            //     + "\r\necPub: " + Convert.ToBase64String(ecPubKey)
            // );

            // var signer = new ACMESharp.Crypto.JOSE.Impl.ESJwsTool();
            // signer.Init();
            // WriteLine($"ECKeys: {signer.Export()}");

            var signer = new PkiJwsTool(256);
            signer.Init();
            var signerExport = signer.Export();
            signer = new PkiJwsTool(256);
            signer.Import(signerExport);
            WriteLine($"ECKeys: {signerExport}");

            var sig1 = signer.Sign(sample);
            WriteLine($"Sig: {Convert.ToBase64String(sig1)}");
            sig1 = signer.Sign(sample);
            WriteLine($"Sig: {Convert.ToBase64String(sig1)}");
            sig1 = signer.Sign(sample);
            WriteLine($"Sig: {Convert.ToBase64String(sig1)}");
            WriteLine($"Vfy: {signer.Verify(sample, sig1)}");
            WriteLine($"JWK: {JsonConvert.SerializeObject(signer.ExportJwk())}");
            WriteLine("Sig1Hex: " + BitConverter.ToString(sig1));

            // var ecAlt = JsonConvert.SerializeObject(signer.KeyPair.ExportEcParameters());
            // WriteLine($"ECAlt:  {ecAlt}");
            // var signer2 = new ACMESharp.Crypto.JOSE.Impl.ESJwsTool();
            // signer2.HashSize = 256;
            // signer2.Init();
            // signer2.Import(ecAlt);

            // WriteLine($"ECKeys2: {signer2.Export()}");
            // var sig2 = signer2.Sign(sample);
            // WriteLine($"Sig2: {Convert.ToBase64String(sig2)}");
            // sig2 = signer2.Sign(sample);
            // WriteLine($"Sig2: {Convert.ToBase64String(sig2)}");
            // sig2 = signer2.Sign(sample);
            // WriteLine($"Sig2: {Convert.ToBase64String(sig2)}");
            // WriteLine($"Vfy2: {signer2.Verify(sample, sig2)}");
            // WriteLine($"JWK2: {JsonConvert.SerializeObject(signer2.ExportJwk())}");
            // WriteLine("Sig2Hex: " + BitConverter.ToString(sig2));

            var lineSeps = "\r\n".ToCharArray();

            using (var acme = new AcmeProtocolClient(_http, signer: signer))
            {
                acme.BeforeAcmeSign = (s,o) => WriteLine($"BEFORE_SIGN({s}, {JsonConvert.SerializeObject(o)})");
                acme.BeforeHttpSend = (s,m) => WriteLine($"BEFORE_SEND({s}, {m.Content?.ReadAsStringAsync().Result}");


                var dir = await acme.GetDirectoryAsync();
                acme.Directory = dir;

                await acme.GetNonceAsync();

                var c = _contacts.Split(lineSeps, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());
                var acct = await acme.CreateAccountAsync(c, _agreeTos);
                var acctStr = JsonConvert.SerializeObject(acct, Formatting.Indented);

                Console.WriteLine("Got Account: " + acctStr);
            }

            signer = new PkiJwsTool(256);
            signer.Import(signerExport);

            using (var acme = new AcmeProtocolClient(_http, signer: signer))
            {
                acme.BeforeAcmeSign = (s,o) => WriteLine($"BEFORE_SIGN({s}, {JsonConvert.SerializeObject(o)})");
                acme.BeforeHttpSend = (s,m) => WriteLine($"BEFORE_SEND({s}, {m.Content?.ReadAsStringAsync().Result}");

                var dir = await acme.GetDirectoryAsync();
                acme.Directory = dir;

                await acme.GetNonceAsync();

                var d = _dnsNames.Split(lineSeps, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());
                var order = await acme.CreateOrderAsync(d);
                var orderStr = JsonConvert.SerializeObject(order, Formatting.Indented);

                Console.WriteLine("Got Order: " + orderStr);
            }
        }
    }

    class ProgramX
    {
        static Label _cwd;

        static void MainX(string[] args)
        {
            Application.Init();

            var top = Application.Top;

            var win = new Window(new Rect(0, 1, top.Frame.Width, top.Frame.Height - 1), "ACMETerm");
            top.Add(win);

            // Creates a menubar, the item "New" has a help menu.
            var menu = new MenuBar(new MenuBarItem[] {
                new MenuBarItem ("_File", new MenuItem [] {
                    new MenuItem ("_New Account", "Creates a new ACME registration Account", NewAccount),
                    new MenuItem ("_Open Account", "Open an existing ACME registration Account", OpenAccount),
                    new MenuItem ("_Close Account", "Close existing ACME registration Account", CloseAccount),
                    new MenuItem ("_Quit", "", () => ClickQuit())
                }),
                new MenuBarItem ("_Edit", new MenuItem [] {
                    new MenuItem ("_Copy", "", null),
                    new MenuItem ("C_ut", "", null),
                    new MenuItem ("_Paste", "", null)
                })
            });
            top.Add(menu);

            // Add some controls
            var ml = new Label(new Rect(3,17, 47, 1), "Mouse: ");
            _cwd = new Label(3, 20, "Current Dir Goes here");
            win.Add(
                    new Label(3, 2, "Login: "),
                    new TextField(14, 2, 40, ""),
                    new Label(3, 4, "Password: "),
                    new TextField(14, 4, 40, "") { Secret = true },
                    new CheckBox(3, 6, "Remember me"),
                    new RadioGroup(3, 8, new[] { "_Personal", "_Company" }),
                    new Button(3, 14, "Ok"),
                    new Button(10, 14, "Cancel"),
                    new Label(3, 18, "Press ESC and 9 to activate the menubar"),
                    ml, _cwd);

            int count = 0;
            Application.RootMouseEvent += delegate (MouseEvent ev) {
                ml.Text =  $"Mouse: ({ev.X},{ev.Y}) - {ev.Flags} {count++}";
            };

            Application.Run();

            void ClickQuit()
            {
                if (Quit ())
                {
                    //top.Running = false;
                    Application.RequestStop();
                }
            }
        }

        static void NewAccount()
        {
            var openDlg = new OpenDialog("New Account", "Select a directory to save your Account details:")
            {
                CanChooseDirectories = true,
                CanChooseFiles = false,
                CanCreateDirectories = true,
                DirectoryPath = Directory.GetCurrentDirectory(),
                FilePath = null,

            };

            Application.Run(openDlg);

            // _cwd.Text = "CWD: " + openDlg.DirectoryPath;
        }

        static void OpenAccount()
        {
        }

        static void CloseAccount()
        { 
        }

        static bool Quit()
        {
            var n = MessageBox.Query (50, 7, "Quit ACMETerm", "Are you sure you want to quit this app?", "Yes", "No");
		    return n == 0;
        }
    }
}
