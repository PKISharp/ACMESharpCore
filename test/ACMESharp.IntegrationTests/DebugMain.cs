using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ACMESharp.IntegrationTests
{
    public class DebugMain
    {
        public async static Task Main(string[] args)
        {
            Console.WriteLine("Debugging: " + typeof(AcmeAccountTests));
            await DebugMain.Run<AcmeAccountTests>();
        }

        public async static Task Run<T>() where T : class
        {
            var conObjects = new List<object>
            {
                new MyTestOutputHelper(),
            };

            var t = typeof(T);

            var con = t.GetConstructors();
            if (con.Length != 1)
                throw new Exception("Class under test should have exactly 1 constructor");

            var ifaces = t.GetInterfaces();
            foreach (var ifc in ifaces)
            {
                if (ifc.IsGenericType)
                {
                    var gtd = ifc.GetGenericTypeDefinition();
                    if (gtd == typeof(IClassFixture<>))
                    {
                        var gtp = ifc.GenericTypeArguments[0];
                        Console.WriteLine("Constructing Class Fixture: " + gtp);
                        conObjects.Add(Activator.CreateInstance(gtp));
                    }
                }
            }

            var conParams = new List<object>();
            foreach (var p in con[0].GetParameters())
            {
                var cp = conObjects.FirstOrDefault(x =>
                        p.ParameterType.IsAssignableFrom(x.GetType()));
                if (cp == null)
                    throw new Exception("No resolved fixtures available to assign to constructor parameter: " + p.Name);
                conParams.Add(cp);
            }

            Console.WriteLine("Constructing test class instance");
            var testInstance = Activator.CreateInstance(typeof(T), conParams.ToArray());
            foreach (var m in t.GetMethods())
            {
                var factAttr = m.GetCustomAttribute<FactAttribute>();
                if (factAttr == null)
                    continue;

                if (!string.IsNullOrEmpty(factAttr.Skip))
                {
                    Console.WriteLine($"SKIPPING[{factAttr.DisplayName ?? m.Name}]: {factAttr.Skip}");
                    continue;
                }

                Console.WriteLine($"RUNNING[{factAttr.DisplayName ?? m.Name}]:");
                try
                {
                    var mret = m.Invoke(testInstance, null);
                    if (mret is Task)
                    {
                        Console.WriteLine("    AWAITING...");
                        await (Task)mret;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("METHOD THREW EXCEPTION: " + ex);
                    return;
                }
            }
        }
    }

    public class MyTestOutputHelper : ITestOutputHelper
    {
        void ITestOutputHelper.WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        void ITestOutputHelper.WriteLine(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }
    }
}