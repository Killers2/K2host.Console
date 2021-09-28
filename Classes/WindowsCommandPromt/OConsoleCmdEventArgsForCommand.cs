/*
' /====================================================\
'| Developed Tony N. Hyde (www.k2host.co.uk)            |
'| Projected Started: 2020-03-16                        | 
'| Use: General                                         |
' \====================================================/
*/
using System;

namespace K2host.Console.Classes
{

    public class OConsoleCmdEventArgsForCommand : EventArgs
    {
        public string OutputData { get; internal set; }
    }

}
