/*
' /====================================================\
'| Developed Tony N. Hyde (www.k2host.co.uk)            |
'| Projected Started: 2020-03-16                        | 
'| Use: General                                         |
' \====================================================/
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using K2host.Console.Classes;
using K2host.Console.Classes.CommandParsing;
using K2host.Console.Delegates;
using K2host.Threading.Interface;

namespace K2host.Console.Interfaces
{

    public interface IConsole : IDisposable
    {

        string Version { get; }

        string CommandPrefix { get; set; }

        bool Running { get; set; }

        bool DisplayCommands { get; set; }

        bool DisplayPrints { get; set; }

        bool PasswordKeys { get; set; }

        IThreadManager ThreadManager { get; }

        OCommandParser CommandParser { get; }

        OnQuestionCallback OnQuestion { get; }

        OnPasswordCallBack OnPassword { get; }

        OnRunStringCallback OnRunString { get; }

        OnRunOutputCallback OnRunOutput { get;}

        ConsoleActionEventHandler ConsoleAction { get; set; }

        LoadInManagersEventHandler LoadInManagers { get; set; }

        Dictionary<string, IConsoleCommand> CommandManager { get; }

        IConsoleCommand RedirectCommand { get; set; }

        string EnvironmentPath { get; set; }

        OConsoleEditor TextEditor { get; }

        bool TextEditorEnabled { get; set; }

        SaveEventHandler OnTextEditorSaving { get; set; }

        FileSystemWatcher ConfigWatcher { get; }

        string ConfigFile { get; }

        StringBuilder PasswordBuilder { get; }

        void Start(string[] args);

        void Hibernate();

        void Run(string command);

        void Run(Output output);

        void Add(IConsoleCommand e);

        void Remove(IConsoleCommand e);

        void Print(string text, ConsoleColor color = ConsoleColor.Gray, bool newline = true, bool endcommand = false);

        void SetupQuestionAnswer(string message, OnQuestionCallback e);

        void AwaitQuestionAnswer();

        void ConfigurationSave();

        void ConfigurationLoad();

    }

}
