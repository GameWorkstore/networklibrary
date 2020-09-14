using System;
using System.Linq;

namespace GameWorkstore.NetworkLibrary
{
    public static class EnviromentUtils
    {
        /// <summary>
        /// Verifies if enviroment argument exists
        /// </summary>
        /// <param name="argument">An argument</param>
        /// <returns>if exists</returns>
        public static bool Exists(string argument) { return Environment.GetCommandLineArgs().Contains(argument); }
        
        /// <summary>
        /// Format and outputs argument and value for easy process spawn usage.
        /// </summary>
        /// <param name="argument">An argument</param>
        /// <param name="value">An value</param>
        /// <returns>formatted argument</returns>
        public static string Format(string argument, string value) { return string.Format(" {0} {1}", argument, value); }

        /// <summary>
        /// Verifies and returns if argument exists and it's value
        /// </summary>
        /// <param name="argument"></param>
        /// <param name="value">output value of argument</param>
        /// <returns>if exists and value</returns>
        public static bool GetValue(string argument, out string value)
        {
            value = string.Empty;
            string[] vars = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(vars, argument);
            if(index >= 0 && (index + 1) < vars.Length)
            {
                value = vars[index + 1];
                return true;
            }
            return false;
        }
    }
}
