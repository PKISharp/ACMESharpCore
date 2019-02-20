using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using ACMESharp.Testing.Xunit;
using Newtonsoft.Json;

namespace ACMESharp.IntegrationTests
{
    public abstract class IntegrationTest
    {
        public IntegrationTest(StateFixture state, ClientsFixture clients)
        {
            State = state;
            Clients = clients;
        }

        protected StateFixture State { get; }

        protected ClientsFixture Clients { get; }

        protected CallerContext LastContext { get; set; }

        protected CallerContext SetTestContext(
                int subseq = -1,
                [System.Runtime.CompilerServices.CallerMemberName]string caller = "")
        {
            var m = this.GetType().GetMember(caller);
            if (m.Length != 1)
                throw new InvalidOperationException("Unable to resolve single member from caller name");

            LastContext = new CallerContext
            {
                Test = this,
                Name = caller,
                Member = m[0],
                TestOrder = TestOrderer.GetTestOrder(m[0]),
                TestGroup = TestOrderer.GetTestGroup(m[0]),
                Subsequence = subseq,
                State = State,
            };

            if (Clients?.Acme != null)
            {
                Clients.Acme.BeforeAcmeSign = BeforeAcmeSign;
                Clients.Acme.BeforeHttpSend = BeforeAcmeSend;
                Clients.Acme.AfterHttpSend = AfterAcmeSend;
            }

            return LastContext;
        }

        protected void BeforeAcmeSign(string opName, object acmeInput)
        {
            var toName = $"{opName}-AcmeInput.json";
            if (acmeInput == null)
                LastContext.WriteTo(toName, "");
            else
                LastContext.WriteTo(toName, JsonConvert.SerializeObject(acmeInput));
        }

        protected void BeforeAcmeSend(string opName, HttpRequestMessage requ)
        {
            var toName = $"{opName}-HttpRequest.json";
            LastContext.WriteTo(toName, $"// {requ.Method} {requ.Version} {requ.RequestUri}\r\n");
            var headers = (requ.Content == null ? requ.Headers : requ.Headers.Concat(requ.Content.Headers))
                .Select(x => $"// {x.Key}: {string.Join(",", x.Value)}");
            LastContext.AppendTo(toName, string.Join("\r\n", headers));
            if (requ.Content != null)
                LastContext.AppendTo(toName, "\r\n" + requ.Content.ReadAsStringAsync().Result);
        }

        protected void AfterAcmeSend(string opName, HttpResponseMessage resp)
        {
            var toName = $"{opName}-HttpResponse.json";
            LastContext.WriteTo(toName, string.Join("\r\n", "// " + resp.StatusCode + "\r\n"));
            var headers = resp.Headers.Concat(resp.Content.Headers)
                .Select(x => $"// {x.Key}: {string.Join(",", x.Value)}");
            LastContext.AppendTo(toName, string.Join("\r\n", headers));
            LastContext.AppendTo(toName, "\r\n" + resp.Content.ReadAsStringAsync().Result);
        }

        public void WriteTo(string saveName, byte[] value, int subseq = -1)
        {
            State.WriteTo($"{ComputePrefix(subseq)}-{saveName}", value);
        }

        public void WriteTo(string saveName, string value, int subseq = -1)
        {
            State.WriteTo($"{ComputePrefix(subseq)}-{saveName}", value);
        }

        public void AppendTo(string saveName, string value, int subseq = -1)
        {
            State.AppendTo($"{ComputePrefix(subseq)}-{saveName}", value);
        }

        public string ReadFrom(string saveName, int subseq = -1)
        {
            return State.ReadFrom($"{ComputePrefix(subseq)}-{saveName}");
        }

        public void ReadFrom(string saveName, out byte[] value, int subseq = -1)
        {
            State.ReadFrom($"{ComputePrefix(subseq)}-{saveName}", out value);
        }

        public void SaveObject(string saveName, object o, int subseq = -1)
        {
            State.SaveObject($"{ComputePrefix(subseq)}-{saveName}", o);
        }
        
        public T LoadObject<T>(string saveName, int subseq = -1)
        {
            return State.LoadObject<T>($"{ComputePrefix(subseq)}-{saveName}");
        }

        private string ComputePrefix(int subseq = -1)
        {
            var to = TestOrderer.GetTestOrder(this);
            var nm = this.GetType().Name;

            var pfx = $"{to:D3}-{nm}";
            if (subseq >= 0)
                pfx += $"-{subseq}";
            return pfx;                
        }

        public class CallerContext
        {
            public IntegrationTest Test { get; set; }

            public string Name { get; set; }

            public MemberInfo Member { get; set; }

            public int TestOrder { get; set; }

            public string TestGroup { get; set; }

            public int Subsequence { get; set; } = -1;

            public StateFixture State { get; set; }

            public void WriteTo(string saveName, byte[] value)
            {
                Test.WriteTo($"{ComputePrefix()}-{saveName}", value);
            }

            public void WriteTo(string saveName, string value)
            {
                Test.WriteTo($"{ComputePrefix()}-{saveName}", value);
            }

            public void AppendTo(string saveName, string value)
            {
                Test.AppendTo($"{ComputePrefix()}-{saveName}", value);
            }

            public string ReadFrom(string saveName)
            {
                return Test.ReadFrom($"{ComputePrefix()}-{saveName}");
            }

            public void SaveObject(string saveName, object o)
            {
                Test.SaveObject($"{ComputePrefix()}-{saveName}", o);
            }

            public T LoadObject<T>(string saveName)
            {
                return Test.LoadObject<T>($"{ComputePrefix()}-{saveName}");
            }

            public void GroupWriteTo(string saveName, byte[] value)
            {
                Test.WriteTo($"{ComputeGroupPrefix()}{saveName}", value);
            }

            public void GroupWriteTo(string saveName, string value)
            {
                Test.WriteTo($"{ComputeGroupPrefix()}{saveName}", value);
            }

            public void GroupAppendTo(string saveName, string value)
            {
                Test.AppendTo($"{ComputeGroupPrefix()}{saveName}", value);
            }

            public string GroupReadFrom(string saveName)
            {
                return Test.ReadFrom($"{ComputeGroupPrefix()}{saveName}");
            }

            public void GroupReadFrom(string saveName, out byte[] value)
            {
                Test.ReadFrom($"{ComputeGroupPrefix()}{saveName}", out value);
            }

            public void GroupSaveObject(string saveName, object o)
            {
                Test.SaveObject($"{ComputeGroupPrefix()}{saveName}", o);
            }

            public T GroupLoadObject<T>(string saveName)
            {
                return Test.LoadObject<T>($"{ComputeGroupPrefix()}{saveName}");
            }

            private string ComputeGroupPrefix()
            {
                if (TestGroup != null)
                    return $"{TestGroup}_";
                else
                    return string.Empty;
            }

            private string ComputePrefix()
            {
                var pfx = TestOrder.ToString("D4");
                if (Subsequence >= 0)
                    pfx += $"-{Subsequence}";
                if (TestGroup != null)
                    pfx += $"_{TestGroup}";
                return pfx;                
            }
        }        
    }
}