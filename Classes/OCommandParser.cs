/*
' /====================================================\
'| Developed Tony N. Hyde (www.k2host.co.uk)            |
'| Projected Started: 2020-03-16                        | 
'| Use: General                                         |
' \====================================================/
*/
using System;
using System.Collections.Generic;

namespace K2host.Console.Classes
{

    public class OCommandParser : IDisposable
    {

        public char[] CommandDelimitors { get; set; }
        public char[] SubCommandDelimitors { get; set; }

        public OCommandParser() 
        {
            CommandDelimitors       = new char[] { ';' };
            SubCommandDelimitors    = new char[] { '.', ' ' };
        }

        /// <summary>
        /// Splits commands with a semicolon ;
        /// The splits the subcommands with either a dot . or space
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public Output Parse(string e)
        {

            Output ret = new();

            if (e.Trim() == string.Empty)
                return ret;

            ret.OriginalCommand = e;

            try
            {

                List<string> subcommands    = new();
                List<string> stack          = new();

                subcommands.AddRange(e.Split(CommandDelimitors, StringSplitOptions.RemoveEmptyEntries));

                subcommands.ForEach(subcommand => {
                    stack.AddRange(subcommand.Split(SubCommandDelimitors, StringSplitOptions.RemoveEmptyEntries));
                    ret.Commands.Add(new List<string>(stack.ToArray()));
                    stack.Clear();
                });

                stack.Clear();
                stack = null;

                subcommands.Clear();
                subcommands = null;

            }
            catch (Exception ex)
            {
                ret.Error = ex.ToString();
            }

            return ret;

        }

        bool IsDisposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (disposing)
            {


            }

            IsDisposed = true;
        }

    }

}
