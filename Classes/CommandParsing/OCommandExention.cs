/*
' /====================================================\
'| Developed Tony N. Hyde (www.k2host.co.uk)            |
'| Projected Started: 2019-03-26                        | 
'| Use: General                                         |
' \====================================================/
*/

using System.ComponentModel;
using K2host.Console.Enums;

namespace K2host.Console.Classes
{

    public class OCommandExention : DescriptionAttribute
    {      

        public OConsoleExentionType ExentionType { get; }

        public OCommandExention(OConsoleExentionType type, string parameters) 
            : base(parameters)
        {
            ExentionType = type;
        }     

    }


}
