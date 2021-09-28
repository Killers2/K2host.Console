/*
' /====================================================\
'| Developed Tony N. Hyde (www.k2host.co.uk)            |
'| Projected Started: 2020-03-16                        | 
'| Use: General                                         |
' \====================================================/
*/
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace K2host.Console.Classes
{

    public class OCommandParser : IDisposable
    {

        public string CommandDelimitor { get; set; } = ";";

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

                subcommands.AddRange(Regex.Split(e, CommandDelimitor + "(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)"));

                subcommands.ForEach(subcommand => {
                   
                    stack.AddRange(Regex.Split(subcommand, " (?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)"));
                   
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
