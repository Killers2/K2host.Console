/*
' /====================================================\
'| Developed Tony N. Hyde (www.k2host.co.uk)            |
'| Projected Started: 2020-03-16                        | 
'| Use: General                                         |
' \====================================================/
*/

using System;
using System.Collections.Generic;

using K2host.Console.Classes;
using K2host.Console.Delegates;

namespace K2host.Console.Interfaces
{

    public interface IConsoleCommand : IDisposable
    {

        IConsole Parent { get; set; }

        OnPrintEventHandler OnPrint { get; set; }

        string Cmd { get; }

        string HelpSyntax { get; }

        string Description { get; }

        Dictionary<string, IConsoleComponent> Components { get; }

        string List();

        void Parse(Output e);

        bool Run(string key, Output output, out object obj);

        void LoadConfiguration(string json);

        string SaveConfiguration();


    }

}
