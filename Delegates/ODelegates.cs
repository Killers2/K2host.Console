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
using K2host.Console.Classes.CommandParsing;
using K2host.Console.Enums;
using K2host.Console.Interfaces;

namespace K2host.Console.Delegates
{
    
    public delegate IEnumerable<IConsoleCommand> LoadInManagersEventHandler();

    public delegate object CallbackAsFunction(Output output);

    public delegate void CallbackAsCommand(Output output);

    public delegate void OnPrintEventHandler(string text, ConsoleColor color = ConsoleColor.Gray, bool newline = true, bool endcommand = false);

    public delegate void OnRunStringCallback(string command);

    public delegate void OnRunOutputCallback(Output output);

    public delegate void OnQuestionCallback(OQuestionAnswer output);

    public delegate void OnPasswordCallBack(bool isValid, string username, string password);

    public delegate void ConsoleActionEventHandler(OConsoleState e);
  
    public delegate void OutputEventHandler(object sendingProcess, OConsoleCmdEventArgsForCommand e);

    public delegate void CloseEventHandler();

    public delegate void SaveEventHandler(OConsoleEditorBuffer buffer);

}
