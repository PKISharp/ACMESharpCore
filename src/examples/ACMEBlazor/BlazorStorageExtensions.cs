using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blazor.Extensions;
using Microsoft.AspNetCore.Blazor.Browser.Interop;

namespace ACMEBlazor
{
    // Temporary fix until this is merged in:
    //    https://github.com/BlazorExtensions/Storage/pull/5
    public static class BlazorStorageExtensions
    {
        public const string LENGTH_METHOD = "Blazor.Extensions.Storage.Length";
        public const string LOCAL_STORAGE = "localStorage";

        public static int GetLength(this LocalStorage local)
        {
            return RegisteredFunction.Invoke<int>(LENGTH_METHOD, LOCAL_STORAGE);
        }
    }
}
