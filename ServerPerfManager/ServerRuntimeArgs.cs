#if UNITY_SERVER || UNITY_EDITOR
using System;
using UnityEngine;

namespace ServerRuntime
{
    /// <summary>Lightweight command line + env reader. Server-only, ignored by clients.</summary>
    public static class ServerRuntimeArgs
    {
        public static string TryGet(string key)
        {
            if (!Application.isBatchMode && !Application.isEditor) return null;

            string[] args = Environment.GetCommandLineArgs();
            string needle1 = "-" + key.ToLowerInvariant() + "=";
            string needle2 = "--" + key.ToLowerInvariant() + "=";

            for (int i = 0; i < args.Length; i++)
            {
                var a = args[i];
                if (a == null) continue;
                var al = a.ToLowerInvariant();
                if (al.StartsWith(needle1) || al.StartsWith(needle2))
                    return a.Substring(a.IndexOf('=') + 1);
            }
            return null;
        }

        public static string TryGetEnv(string envVar)
        {
            if (!Application.isBatchMode && !Application.isEditor) return null;
            return Environment.GetEnvironmentVariable(envVar);
        }

        public static bool TryParseOnOff(string value, out bool result)
        {
            result = false;
            if (string.IsNullOrEmpty(value)) return false;
            switch (value.Trim().ToLowerInvariant())
            {
                case "1": case "true": case "on": case "yes": case "y": result = true; return true;
                case "0": case "false": case "off": case "no": case "n": result = false; return true;
                default: return false;
            }
        }
    }
}
#endif
