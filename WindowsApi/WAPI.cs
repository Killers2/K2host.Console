/*
' /====================================================\
'| Developed Tony N. Hyde (www.k2host.co.uk)            |
'| Projected Started: 2020-03-16                        | 
'| Use: General                                         |
' \====================================================/
*/

using System;
using System.Runtime.InteropServices;

namespace K2host.Console.WindowsApi
{
    public static class WAPI
    {

        [DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern long SetConsoleTextAttribute(int hConsoleOutput, int wScreenColors);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GetStdHandle(int nStdHandle);


    }
}
