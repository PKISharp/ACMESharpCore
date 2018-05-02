using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using ACMESharp.Testing.Xunit;
using Newtonsoft.Json;

namespace ACMESharp.IntegrationTests
{
    public class StateFixture
    {
        public StateFixture()
        {
            // Need a place to stash stuff
            if (!Directory.Exists("_TMP"))
                Directory.CreateDirectory("_TMP");
        }

        public void WriteTo(string saveName, string value)
        {
            File.WriteAllText($"_TMP\\{saveName}", value);
        }

        public void AppendTo(string saveName, string value)
        {
            File.AppendAllText($"_TMP\\{saveName}", value);
        }

        public string ReadFrom(string saveName)
        {
            var fromName = $"_TMP\\{saveName}";
            if (File.Exists(fromName))
                return File.ReadAllText(fromName);
            
            return null;
        }

        public void SaveObject(string saveName, object o)
        {
            var json = JsonConvert.SerializeObject(o, Formatting.Indented);
            WriteTo(saveName, json);
        }

        public T LoadObject<T>(string saveName)
        {
            var json = ReadFrom(saveName);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}