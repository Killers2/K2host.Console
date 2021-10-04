/*
' /====================================================\
'| Developed Tony N. Hyde (www.k2host.co.uk)            |
'| Projected Started: 2020-03-16                        | 
'| Use: General                                         |
' \====================================================/
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace K2host.Console.Classes
{

    public class OCommandParser : IDisposable
    {

        public char[] CommandDelimiters { get; set; } = new char[] { ';' };

        public char[] SubCommandDelimiters { get; set; } = new char[] { '.', ' ' };
        
        public OCommandParser() { }
        
        public Output Parse(string e)
        {

            Output ret = new();

            if (e == null || e.Trim() == string.Empty)
                return ret;

            ret.OriginalCommand = e;

            try
            {
                List<string> subcommands    = new();
                List<string> stack          = new();

                //We split the command in to subcommands first.
                subcommands
                    .AddRange(e.Split(CommandDelimiters, StringSplitOptions.RemoveEmptyEntries));

                //Now lets build sub commands for each stack
                subcommands
                    .ForEach(subcommand => {
                        stack.AddRange(
                            subcommand
                                .Split('"') // allow quotes not to split ie anything in quotes will be one string, where the rest is split
                                .Select((element, index) => index % 2 == 0 ? element.Split(SubCommandDelimiters, StringSplitOptions.RemoveEmptyEntries) : new string[] { element })
                                .SelectMany(element => element)
                                .ToArray()
                        );

                        ret.Commands.Add(new List<string>(stack.ToArray()));
                        stack.Clear();                
                    });

                stack.Clear();
                subcommands.Clear();

                stack = null;
                subcommands = null;

            }
            catch (Exception ex)
            {
                ret.Error = ex.ToString();
            }

            return ret;

        }

        #region "Destructor"

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

        #endregion
    }

}
