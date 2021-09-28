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

namespace K2host.Console.Classes
{

    public class Output : IDisposable
    {

        public List<List<string>> Commands { get; }

        public string Error { get; set; }

        public int WaitTime { get; set; }

        public string OriginalCommand { get; set; }

        public Output()
        {
            Commands = new List<List<string>>();
        }

        public Output(List<string> subcommands)
            : this()
        {
            Commands = new List<List<string>> { subcommands };
        }

        public Output(List<List<string>> commands)
            : this()
        {
            Commands = commands;
        }

        public int GetStackIndex(string subcommand)
        {
            for (var i = 0; i < Commands.Count; i++)
                if (Commands[i][0] == subcommand)
                    return i;
            return -1;
        }

        public List<string> GetStack(string subcommand)
        {
            return Commands.Where(c => c[0] == subcommand).FirstOrDefault();
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

                foreach (List<string> l in Commands)
                    l.Clear();

                Commands.Clear();

            }

            IsDisposed = true;
        }

        #endregion

    }

}
