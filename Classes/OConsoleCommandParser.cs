/*
' /====================================================\
'| Developed Tony N. Hyde (www.k2host.co.uk)            |
'| Projected Started: 2020-03-16                        | 
'| Use: General                                         |
' \====================================================/
*/

using System;
using System.Collections.Generic;

using Microsoft.VisualBasic;

using K2host.Console.Enums;

using gl = K2host.Console.WindowsApi.WAPI;

namespace K2host.Console.Classes
{

    public static class OConsoleCommandParser
    {

        private const int STD_OUTPUT_HANDLE = -11;

        public static void SetConsoleColors(ConsoleColors forecolor, ConsoleColors backcolor)
        {
            IntPtr hConsole = gl.GetStdHandle(STD_OUTPUT_HANDLE);
            backcolor = (ConsoleColors)((int)backcolor == 0 ? 256 : ((int)backcolor * 16));
            gl.SetConsoleTextAttribute((int)hConsole, (int)forecolor | (int)backcolor);
        }

        public static void SetConsoleColors(ConsoleColors forecolor)
        {
            IntPtr hConsole = gl.GetStdHandle(STD_OUTPUT_HANDLE);
            gl.SetConsoleTextAttribute((int)hConsole, (int)forecolor);
        }

        public static void PrintToConsole(string Text, ConsoleColors ForeColor, bool NoCRLF)
        {

            SetConsoleColors(ForeColor);

            if (!NoCRLF)
                System.Console.Write(Text + "\n");
            else
                System.Console.Write(Text + "\n\n");

            SetConsoleColors(ConsoleColors.Grey);

        }

        public static void PrintToConsole(string Text, bool NoCRLF)
        {

            SetConsoleColors(ConsoleColors.White);

            if (!NoCRLF)
                System.Console.Write(Text + "\n");
            else
                System.Console.Write(Text + "\n\n");

            SetConsoleColors(ConsoleColors.Grey);

        }

        public static string[] ToArgs(string CmdString)
        {
            
            string[] Output;
            List<string> ArgList = new();

            CmdString = CmdString.Replace("\\\\", "[%BS%]");
            CmdString = CmdString.Replace("\\\"", "[%Quote%]");
            CmdString = CmdString.Replace("\\n", Constants.vbCrLf);
            CmdString = CmdString.Replace("\\r", Constants.vbCrLf);
            CmdString = CmdString.Replace("\\t", Constants.vbTab);

            if (CountQuotes(CmdString) % 2 != 0)
                Output = new string[] { "!Syntax error: End of string expected!" };
            else
            {

                string  CurrentArgument = "";
                bool    InArg           = false;
                bool    WasQuote        = false;

                for (int i = 0; i <= CmdString.Length - 1; i++)
                {
                    
                    if (CmdString.Substring(i, 1) == " " & !InArg)
                    {
                        if (!WasQuote)
                        {
                            ArgList.Add(RemoveQuotes(CurrentArgument.Trim()));
                            CurrentArgument = "";
                        }
                        else
                            WasQuote = false;
                    }

                    if (CmdString.Substring(i, 1) == "\"")
                    {
                        if (InArg)
                        {
                            ArgList.Add(RemoveQuotes(CurrentArgument.Trim()));
                            CurrentArgument = "";
                            InArg = false;
                            WasQuote = true;
                            continue;
                        }
                        else
                        {
                            InArg = true;
                            continue;
                        }
                    }

                    CurrentArgument += CmdString.Substring(i, 1);

                }

                if (CmdString.Trim().Substring(0, 1) != "\"")
                    ArgList.Add(RemoveQuotes(CurrentArgument.Trim()));

                for (int i = 0; i <= ArgList.Count - 1; i++)
                {
                    ArgList[i] = ArgList[i].Replace("[%BS%]", "\\");
                    ArgList[i] = ArgList[i].Replace("[%Quote%]", "\"");
                    ArgList[i] = ArgList[i].Replace("[%SingleQuote%]", "'");
                }

                Output      = new string[ArgList.Count + 1];
                Output[0]   = "?Result";

                for (int i = 0; i <= ArgList.Count - 1; i++)
                    Output[i + 1] = ArgList[i];

            }

            return Output;

        }

        public static int CountQuotes(string Text)
        {
            int c = 0;
            for (int i = 0; i <= Text.Length - 1; i++)
            {
                if (Text.Substring(i, 1) == "\"")
                    c += 1;
            }
            return c;
        }

        public static string RemoveQuotes(string Text)
        {
            string b = Text;
            if (Strings.Left(b, 1) == "\"")
                b = Strings.Right(b, Strings.Len(b) - 1);
            if (Strings.Right(b, 1) == "\"")
                b = Strings.Left(b, Strings.Len(b) - 1);
            return b;
        }

        public static string ReadArgv(string Command, int Arg)
        {
            try
            {
                string[] Args = ToArgs(Command);
                return Args[Arg + 1];
            }
            catch
            {
                return null;
            }
        }

        public static int CountArgs(string Command)
        {
            string[] Args = ToArgs(Command);
            return Args.Length - 1;
        }

        public static bool ParamOk(string Command, int Arg, ref string ErrorValue)
        {
            string[] Args = ToArgs(Command);
            if (Args[0].Substring(0, 1) == "!")
            {
                ErrorValue = Strings.Right(Args[0], Args[0].Length - 1);
                return false;
            }
            else
            {
                if (Arg + 2 >= Args.Length)
                {
                    ErrorValue = "No such parameter!";
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

    }


}
