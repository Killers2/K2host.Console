/*
' /====================================================\
'| Developed Tony N. Hyde (www.k2host.co.uk)            |
'| Projected Started: 2020-03-16                        | 
'| Use: General                                         |
' \====================================================/
*/

using System;

namespace K2host.Console.Interfaces
{

    public interface IConsoleComponent : IDisposable
    {

        string CommandKey { get; set; }

        string Description { get; set; }

        string SyntaxHelp { get; set; }

    }

}
