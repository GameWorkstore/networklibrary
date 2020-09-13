using System;
using System.Collections;
using System.Linq;

namespace UnityEngine.NetLibrary.Scalability
{
    public static class EnviromentUtils
    {
        public const int CENTRAL_PORT = 5000;
        public const int CENTRAL_MATCHSIZE = 1000;
        public const int SPAWNER_PORT = 5001;
        public const int SPAWNER_MATCHSIZE = 1000;
        public const int DEFAULT_GAME_PORT = 7000;

        public const string MATCHMAKING_ARG = "-matchmaking";
        public const string MATCHID_ARG = "-matchid";
        public const string PORT_ARG = "-port";
        public const string MATCHSIZE_ARG = "-matchsize";
        public const string BOTS_ARG = "-botsize";
        //public const string CENTRAL_IP = "-ipcentral";
        public const string SPAWNER_IP = "-ipspawner";

        public const string StandardBatchModeArgs = MATCHMAKING_ARG + " -batchmode -nographics -logfile";
        public const string StandardtArgs = MATCHMAKING_ARG +" -logfile -screen-width 320 -screen-height 240";

        public static bool ArgProvided(string arg) { return Environment.GetCommandLineArgs().Contains(arg); }

        public static string ArgsFormat(string arg, string obj) { return string.Format(" {0} {1}", arg, obj); }

        public static bool GetArgProvided(string arg, out string value)
        {
            value = string.Empty;
            string[] vars = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(vars, arg);
            if(index >= 0 && (index + 1) < vars.Length)
            {
                value = vars[index + 1];
                return true;
            }
            return false;
        }

        public static IEnumerator SendRequest(string uri, WWWForm form, Action<bool, string> result)
        {
            var www = new WWW(uri, form);
            yield return www;

            if (string.IsNullOrEmpty(www.error))
            {
                result(true, www.text);
            }
            else
            {
                result(false, www.error);
            }
        }
    }
}
