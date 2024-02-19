using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ACMESharp.IntegrationTests
{
    public class StateFixture
    {
        public StateFixture()
        {
            // Need a place to stash stuff
            if (!Directory.Exists("_TMP"))
                Directory.CreateDirectory("_TMP");

            Factory = new LoggerFactory().AddFile("integration-tests.log");
        }

        public ILoggerFactory Factory { get; }

        public Random Rng { get; } = new Random();

        public void WriteTo(string saveName, byte[] value)
        {
            File.WriteAllBytes($"_TMP/{saveName}", value);
        }

        public void WriteTo(string saveName, string value)
        {
            File.WriteAllText($"_TMP/{saveName}", value);
        }

        public void AppendTo(string saveName, string value)
        {
            File.AppendAllText($"_TMP/{saveName}", value);
        }

        public string ReadFrom(string saveName)
        {
            var fromName = $"_TMP/{saveName}";
            if (File.Exists(fromName))
                return File.ReadAllText(fromName);
            
            return null;
        }

        public void ReadFrom(string saveName, out byte[] value)
        {
            var fromName = $"_TMP/{saveName}";
            if (File.Exists(fromName))
                value = File.ReadAllBytes(fromName);
            else
                value = null;
        }

        public void SaveObject(string saveName, object o)
        {
            var json = JsonSerializer.Serialize(o, JsonHelpers.JsonWebIndentedOptions);
            WriteTo(saveName, json);
        }

        public T LoadObject<T>(string saveName)
        {
            var json = ReadFrom(saveName);

            return json == null ? default : JsonSerializer.Deserialize<T>(json, JsonHelpers.JsonWebIndentedOptions);
        }

        public byte[] RandomBytes(int byteLen)
        {
            var bytes = new byte[byteLen];
            Rng.NextBytes(bytes);
            return bytes;
        }

        public string RandomBytesString(int byteLen) =>
            BitConverter.ToString(RandomBytes(byteLen));
    }
}