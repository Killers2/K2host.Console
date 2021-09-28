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
using System.Threading;
using System.IO;
using System.Text;
using System.Security.AccessControl;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using K2host.Core;
using K2host.Threading.Classes;
using K2host.Threading.Extentions;
using K2host.Threading.Interface;
using K2host.Console.Enums;
using K2host.Console.Interfaces;
using K2host.Console.Delegates;
using K2host.Console.Managers;
using K2host.Console.Classes.CommandParsing;

using gl = K2host.Core.OHelpers;

namespace K2host.Console.Classes
{

    public class OConsoleX : IConsole
    {

        #region Properties

        public string Version { get; } = "5.0.2";

        public string CommandPrefix { get; set; }

        public bool Running { get; set; } = true;

        public bool DisplayCommands { get; set; } = false;

        public bool DisplayPrints { get; set; } = true;
        
        public bool PasswordKeys { get; set; } = false;

        public IThreadManager ThreadManager { get; } = new OThreadManager();

        public OCommandParser CommandParser { get; } = new OCommandParser();

        public OnQuestionCallback OnQuestion { get; private set; }
       
        public OnPasswordCallBack OnPassword { get; private set; }

        public OnRunStringCallback OnRunString { get; private set; }

        public OnRunOutputCallback OnRunOutput { get; private set; }

        public ConsoleActionEventHandler ConsoleAction { get; set; }

        public LoadInManagersEventHandler LoadInManagers { get; set; }

        public Dictionary<string, IConsoleCommand> CommandManager { get; } = new();

        public IConsoleCommand RedirectCommand { get; set; } = null;

        public string EnvironmentPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        public OConsoleEditor TextEditor { get; private set; }

        public bool TextEditorEnabled { get; set; } = false;

        public SaveEventHandler OnTextEditorSaving { get; set; }

        public FileSystemWatcher ConfigWatcher { get; private set; }

        public string ConfigFile { get; private set; } = AppDomain.CurrentDomain.BaseDirectory + "memory.json";

        public StringBuilder PasswordBuilder { get; } = new();
        
        bool WasJustPrinted      = false;

        #endregion

        #region Constructor

        public OConsoleX()
        {

            //System.Console.WindowWidth = 150;
            //System.Console.BufferWidth = System.Console.WindowWidth;

        }

        #endregion

        #region Methods

        public void Start(string[] args) 
        { 

            System.Console.Title = "ConsoleX Terminal";
            System.Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) => { e.Cancel = true; };

            CommandPrefix = Environment.UserName.ToLower().Replace(" ", string.Empty) + "@" + Environment.MachineName.ToLower();

            if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\Installs"))
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "\\Installs");

            OnRunString += new OnRunStringCallback(Run);
            OnRunOutput += new OnRunOutputCallback(Run);

            TextEditor = new(1) {
                OnClose = () =>
                {
                    TextEditorEnabled = false; 
                    OConsoleEditorAnsi.ClearScreen();
                    TextEditor.ClearBuffer();
                    TextEditor.IoFile   = string.Empty;
                    TextEditor.SaveType = string.Empty;
                    Print(string.Empty, ConsoleColor.White, true, true);
                },
                OnSave = (context) => 
                {
                    TextEditorEnabled = false;
                    OConsoleEditorAnsi.ClearScreen();
                    if (!string.IsNullOrEmpty(TextEditor.IoFile))
                    {
                        if (File.Exists(TextEditor.IoFile))
                        {
                            File.WriteAllText(TextEditor.IoFile, context.GetBuffer());
                            Print("Conent saved as - " + TextEditor.IoFile, ConsoleColor.Green, true);
                        }
                    }
                    else
                    {

                        if (TextEditor.SaveType.GetType() == typeof(OManagerVariables.Variable))
                        {
                            var variable = (OManagerVariables.Variable)TextEditor.SaveType;
                            variable.Value = context.GetBuffer();
                            Print("Conent saved as variable: " + variable.CommandKey, ConsoleColor.Green, true);
                        }
                        
                        if (TextEditor.SaveType.GetType() == typeof(OManagerMacros.Macro))
                        {
                            var macro = (OManagerMacros.Macro)TextEditor.SaveType;
                            macro.Code = context.GetBuffer();
                            Print("Conent saved as macro: " + macro.CommandKey, ConsoleColor.Green, true);
                        }

                        OnTextEditorSaving?.Invoke(context);

                    }

                    TextEditor.ClearBuffer();
                    TextEditor.IoFile   = string.Empty;
                    TextEditor.SaveType = string.Empty;
                    Print(string.Empty, ConsoleColor.White, true, true);
                }
            };

            Print("ConsoleX Terminal Interface. \r\nVersion " + Version, ConsoleColor.White, true);

            DisplayPrints = false;

            Add(new OManagerCommands() { Parent = this });
            
            LoadInternalCommands();

            Add(LoadInManagers?.Invoke());

            ConfigWatcher = new(ConfigFile.Remove(ConfigFile.LastIndexOf("\\")))
            {
                NotifyFilter        = NotifyFilters.Size,
                EnableRaisingEvents = true,
                Filter              = ConfigFile.Remove(0, ConfigFile.LastIndexOf("\\") + 1)
            };
            ConfigWatcher.Changed += (sender, e) => {
                DisplayPrints = false;
                ConfigurationLoad();
                DisplayPrints = true;
            };

            ConfigurationLoad();

            DisplayPrints = true;

            if (args != null && args.Length > 0) 
            {
                Print(string.Empty);
                Print("Running Command Args...", ConsoleColor.White, true);
                args.ForEach(command => {
                    if (command.StartsWith("user"))
                         CommandPrefix = command.Remove(0, command.IndexOf(" ") + 1).ToLower() + "@" + Environment.MachineName.ToLower();
                    else
                        Run(command); 
                });
            }

            Print(string.Empty, ConsoleColor.White, true, true);

            while (Running)
                try
                {
                    if (!TextEditorEnabled && !PasswordKeys)
                        Run(System.Console.ReadLine());
                    else if (TextEditorEnabled && !PasswordKeys)
                        Pad();
                    else if (PasswordKeys)
                        Pwd();
                }
                catch (Exception ex)
                {
                    Print("ConsoleX::Exception: " + ex.Message, ConsoleColor.Red, true, true);
                }

        }

        public void Hibernate()
        {
            Print(string.Empty);

            try
            {
                Print("Saving configuration...".Push(2).FixedLength(50), ConsoleColor.White, false);
                PrintStatus(OConsoleStatus.Ok);
                ConfigurationSave();
            }
            catch { PrintStatus(OConsoleStatus.Failed); }

            try
            {
                Print("Unmounting command system...".Push(2).FixedLength(50), ConsoleColor.White, false);
                CommandManager.Values.ForEach(manager => {
                    manager.Components.Values.ForEach(component => {
                        component.Dispose();
                    });
                    manager.Dispose();
                });
                PrintStatus(OConsoleStatus.Ok);
            }
            catch { PrintStatus(OConsoleStatus.Failed); }

            try
            {
                Print("Detached events system...".Push(2).FixedLength(50), ConsoleColor.White, false);
                LoadInManagers  = null;
                OnQuestion      = null;
                ConsoleAction   = null;
                PrintStatus(OConsoleStatus.Ok);
            }
            catch { PrintStatus(OConsoleStatus.Failed); }

            try
            {
                Print(string.Empty);
                Print("Shutting down. Please wait...".FixedLength(50), ConsoleColor.White, false);
                PrintStatus(OConsoleStatus.Ok);
            }
            catch { PrintStatus(OConsoleStatus.Failed); }

            Thread.Sleep(1000);

            Running = false;

        }

        public void Run(string command)
        {
            Run(CommandParser.Parse(command));
        }

        public void Run(Output output) 
        {

            if (DisplayCommands)
            {
                PrintPrefix();
                Print(output.OriginalCommand, ConsoleColor.Gray, true);
            }

            if (RedirectCommand != null && output.Commands[0][0].Trim() != RedirectCommand.Cmd)
            {
                RedirectCommand.Run("redirect", output, out object _);
                output.Dispose();
                return;
            }

            if (OnQuestion != null)
            {
                CommandQuestion(output);
                return;
            }

            if (OnPassword != null)
            {
                CommandPassword(output);
                return;
            }

            OManagerCommands manager = (OManagerCommands)CommandManager.Values.First();
           
            foreach (List<string> subcommand in output.Commands)
            {
                if (manager.Components.ContainsKey(subcommand[0]))
                {
                    manager.Run(subcommand[0], output, out object obj);

                    if (obj != null)
                        Print(subcommand[0] + " returned \"" + obj.ToString() + "\"", ConsoleColor.Yellow, true, true);

                    subcommand[0] = "~" + subcommand[0]; // completed command.
                }
                else
                {
                    if (CommandNotFound(output))
                        Print(subcommand[0].Push(2) + ": UNKNOWN COMMAND", ConsoleColor.Yellow, true, true);
                }

                if (subcommand[0] == "~wait") // completed command.
                    break;
            }

            if (string.IsNullOrEmpty(output.OriginalCommand))
                WasJustPrinted = false;

            PrintPrefix();

        }

        public void Pad()
        {
            TextEditor.Render();
            TextEditor.HandleInput();
        }

        public void Pwd()
        {
            int x = System.Console.CursorLeft;
            int y = System.Console.CursorTop;

            ConsoleKeyInfo key = System.Console.ReadKey(true);

            if (key.Key == ConsoleKey.Enter)
            {
                Run(PasswordBuilder.ToString());
                PasswordBuilder.Clear();
            }

            if (key.Key == ConsoleKey.Backspace && PasswordBuilder.Length > 0)
            {
                PasswordBuilder.Remove(PasswordBuilder.Length - 1, 1);
                System.Console.SetCursorPosition(x - 1, y);
                System.Console.Write(" ");
                System.Console.SetCursorPosition(x - 1, y);
            }
            else if (key.KeyChar < 32 || key.KeyChar > 126)
                Trace.WriteLine("Output suppressed: no key char");
            else if (key.Key != ConsoleKey.Backspace)
            {
                PasswordBuilder.Append(key.KeyChar);
                System.Console.Write("*");
            }
        }

        public void Add(IEnumerable<IConsoleCommand> e)
        {

            if (e == null)
                return;

            e.ForEach(c => { Add(c); });
        }

        public void Add(IConsoleCommand e) 
        {
            if (e == null)
                return;

            if (CommandManager.ContainsKey(e.Cmd))
                return;

            CommandManager.Add(e.Cmd, e);

            e.OnPrint = new OnPrintEventHandler(Print);
           
            ((OManagerCommands)CommandManager.Values.First()).Add(e.Cmd, e.Parse, e.HelpSyntax, e.Description);

        }

        public void Remove(IEnumerable<IConsoleCommand> e)
        {
            if (e == null)
                return;

            e.ForEach(c => { Remove(c); });

        }

        public void Remove(IConsoleCommand e)
        {
            if (e == null)
                return;

            if (!CommandManager.ContainsKey(e.Cmd))
                return;

            CommandManager.Remove(e.Cmd);

            e.OnPrint = null;
            e.Dispose();

        }
      
        private void ExecuteConfigFile(string filename)
        {

            if (filename.Trim() == string.Empty)
                return;

            if (File.Exists(filename))
                try
                {
                    File.ReadAllLines(filename)
                        .ForEach(line => {
                            line = line.Replace("\t", " ").Trim();
                            if ((!string.IsNullOrEmpty(line)) && (!line.StartsWith("//")))
                            {
                                DisplayCommands = false;
                                Run(line);
                                DisplayCommands = true;
                            }
                        });
                }
                catch (Exception ex)
                {
                    Print("Exception::exec config: " + ex.Message, ConsoleColor.Red, true, true);
                }
            else
                Print("File not found: " + filename, ConsoleColor.Red, true, true);

        }

        private void PrintClear()
        {

            System.Console.Clear();

            string clrText = "ConsoleX Terminal Interface. \r\nVersion " + Version;

            Print(clrText, ConsoleColor.White, true, true);

        }

        private void PrintVersion()
        {

            string ver_text = "ConsoleX Terminal Interface " + Version + " was developed in C# .NET Core using the K2host Libraries. \r\n";
            ver_text += "http://www.k2host.co.uk \r\n";
            ver_text += "2002-" + DateTime.Now.Year + " K2host and mCoDev Systems \r\n";
            ver_text += "This ConsoleX library falls under the MIT Licence. \r\n";

            Print(ver_text, ConsoleColor.White, true, true);

        }

        private void PrintHelp()
        {

            IConsoleCommand manager = CommandManager.Values.First();

            Print("Console quick help:");
            Print("---------------------------------------------------------");
            Print("  When an alias, command or function is being sent, the console will try and execute, and output the value(s).");
            Print("  " + CommandParser.CommandDelimitor + "    Use the semicolon to split commands when typing multiple commands on one line.");
            Print(string.Empty);
            Print("List of commands:");
            Print("---------------------------------------------------------");
            Print(manager.List(), ConsoleColor.White, false);
            Print(string.Empty);
            Print("Possible data types for registering a console variable:");
            Print("---------------------------------------------------------");
            Print("Bool, Boolean".Push(2));
            Print("Byte".Push(2));
            Print("Float, Single".Push(2));
            Print("Double".Push(2));
            Print("Short, Int16".Push(2));
            Print("Integer, Int32".Push(2));
            Print("Long, Int64".Push(2));
            Print("String".Push(2));
            Print(string.Empty, ConsoleColor.White, true, true);

        }

        private void PrintStatus(OConsoleStatus e)
        {

            ConsoleColor c = e switch
            {
                OConsoleStatus.Ok => ConsoleColor.Green,
                OConsoleStatus.Moderate => ConsoleColor.Magenta,
                OConsoleStatus.Failed => ConsoleColor.Red,
                _ => ConsoleColor.White,
            };

            Print("[ ", ConsoleColor.White, false);
            Print(e.ToString().ToUpper(), c, false);
            Print(" ]", ConsoleColor.White, true);

        }

        private void PrintPrefix(bool overridePrint = false) 
        {

            if (WasJustPrinted && !overridePrint)
                return;

            Print(CommandPrefix, ConsoleColor.DarkRed, false);
            Print(":", ConsoleColor.White, false);
            Print("~", ConsoleColor.Blue, false);
            Print("$ ", ConsoleColor.White, false);

            WasJustPrinted = true;

        }

        public void Print(string text, ConsoleColor color = ConsoleColor.Gray, bool newline = true, bool endcommand = false)
        {
            
            if (!Running || !DisplayPrints)
                return;

            System.Console.ForegroundColor = color;

            if (!newline)
                System.Console.Write(text);
            else
            {
                System.Console.WriteLine(text);
                if (endcommand)
                {
                    WasJustPrinted = false;
                    PrintPrefix();
                }
            }

            System.Console.ForegroundColor = ConsoleColor.Gray;

        }

        private void LoadInternalCommands()
        {
            //Let grab the command default manager.
            OManagerCommands manager = (OManagerCommands)CommandManager.Values.First();

            //Lets add the default internal commands
            manager.Add("@echo",    CommandAtEcho,      "<OPTION(S)> <on/off>", "Toggles on/off command display.");
            manager.Add("echo",     CommandEcho,        "<STRING>",             "Outputs a string variable.");
            manager.Add("wait",     CommandWait,        "<TIME>",               "Wait 'n' seconds before running the next command.");
            manager.Add("clr",      CommandClear,       "-",                    "Clears the Output.");
            manager.Add("exec",     CommandExecute,     "<FILENAME>",           "Executes a configuration file (.cfg).");
            manager.Add("exit",     CommandExit,        "-",                    "Calls an event to exit. It will most likely also end the program.");
            manager.Add("restart",  CommandRestart,     "-",                    "Calls an event to restart. It will most likely also restart the console.");
            manager.Add("show",     CommandShow,        "<TYPE>",               "Outputs the internal extention methods, functions or services avaliable.");
            manager.Add("help",     CommandHelp,        "-",                    "Outputs this help file to the screen.");
            manager.Add("ver",      CommandVersion,     "-",                    "Outputs the current version of the console core to the screen.");
            manager.Add("pad",      CommandPad,         "<OPTION(S)>",          "Starts the terminal text editor.");
            manager.Add("whoami",   CommandWhoAmI,      "<OPTION(S)>",          "This command displays the current user name.");
            manager.Add("mkdir",    CommandMkDir,       "<OPTION(S)>",          "This command helps to create a new directory.");
            manager.Add("nc",       CommandNew,         "<OPTION(S)>",          "This is used to start another console process.");
            manager.Add("cd",       CommandPath,        "<OPTION(S)>",          "Outputs and sets the current environment path.");
            manager.Add("ls",       CommandDir,         "<OPTION(S)>",          "listing folders and files in the env directory.");
            manager.Add("cp",       CommandCopy,        "<OPTION(S)>",          "This is used for copying file(s) or directories.");
            manager.Add("mv",       CommandMove,        "<OPTION(S)>",          "This is used for moving file(s) or directories.");
            manager.Add("rm",       CommandRemove,      "<OPTION(S)>",          "This is used for deleting file(s) or directories.");

            //Lets load any downloaded / installed managers.
            string installsPath = AppDomain.CurrentDomain.BaseDirectory + "\\Installs";

            //Let go though all the managers directories for the dll lib.
            new DirectoryInfo(installsPath).GetDirectories().ForEach(di => {
                    
                //Let try and load the managers from each dll lib.
                di.GetFiles("*.dll").ForEach(fi => {
                    try
                    {
                        Assembly PlugInAssembly = Assembly.LoadFrom(fi.FullName);
                        Type PlugInType = PlugInAssembly.GetTypes().FirstOrDefault(t => t.GetInterface(nameof(IConsoleCommand)) != null);

                        //If we find an interface then we try and load it in.
                        if (PlugInType != null)
                        {

                            PlugInAssembly
                                .GetReferencedAssemblies()
                                .ForEach(a =>
                                {

                                    //Let try and load any references outside the scope of the console lib
                                    var ck = AppDomain.CurrentDomain
                                            .GetAssemblies()
                                            .Where(v => v.GetName().Name == a.Name)
                                            .FirstOrDefault();

                                    if (ck == null)
                                        try { Assembly.LoadFrom(PlugInAssembly.Location.Remove(PlugInAssembly.Location.LastIndexOf("\\") + 1) + a.Name + ".dll"); } catch { }
                                });

                            //Lets create an instance of the manager.
                            IConsoleCommand pm = (IConsoleCommand)PlugInAssembly.CreateInstance(PlugInType.FullName);

                            //Set the parent of the manager.
                            pm.Parent = this;

                            //Add it to the command manager.
                            Add(pm);

                        }

                    }
                    catch { }
                });

            });

        }
       
        public void SetupQuestionAnswer(string message, OnQuestionCallback e) 
        {
            OnQuestion = e;
            Print(message, ConsoleColor.Yellow, true, true);
        }

        public void AwaitQuestionAnswer()
        {
            CommandQuestion(CommandParser.Parse(System.Console.ReadLine()));
        }

        private void CommandQuestion(Output output)
        {

            OQuestionAnswer val = OQuestionAnswer.None;

            string tmp = output.Commands[0][0].Trim().ToLower().Replace("\"", string.Empty);

            if (tmp == "y" || tmp == "ya" || tmp == "yes")
                val = OQuestionAnswer.Yes;

            if (tmp == "n" || tmp == "na" || tmp == "no")
                val = OQuestionAnswer.No;

            if (val == OQuestionAnswer.None)
            {
                Print("Question:: answer unknown.", ConsoleColor.Red, true, true);
                OnQuestion = null;
                return;
            }

            OnQuestion.Invoke(val);
            OnQuestion = null;

        }

        private void CommandPassword(Output output)
        {

            string userName = output.Commands[0][0].Trim().Replace("\"", string.Empty);
            string password = output.Commands[0][1].Trim().Replace("\"", string.Empty);

            bool isCredentialValid = false;

            try
            {
                
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    isCredentialValid = new PrincipalContext(ContextType.Machine).ValidateCredentials(userName, password);
          
            } 
            catch(Exception ex)
            {
                if (ex.Message.ToLower().Contains("disabled") && userName == "Administrator")
                {
                    isCredentialValid = true;
                    password = userName;
                }
            }

            OnPassword.Invoke(isCredentialValid, userName, password);
            OnPassword = null;
        }

        private void CommandAtEcho(Output output)
        {

            int idx = output.GetStackIndex("@echo");

            if (output.Commands[idx].Count > 1)
                switch (output.Commands[idx][1])
                {
                    case "-on":
                        DisplayCommands = true;
                        Print(string.Empty, ConsoleColor.White, true, true);
                        break;
                    case "-off":
                        DisplayCommands = false;
                        Print(string.Empty, ConsoleColor.White, true, true);
                        break;
                    case "help":
                        Print(string.Empty);
                        Print("help: command \"@echo\"");
                        Print(string.Empty);
                        Print("Command will only toggle the @echo property.");
                        Print("-on".Push(2).FixedLength(10) + "-".FixedLength(10) + "This will toggle the @echo -on.");
                        Print("-off".Push(2).FixedLength(10) + "-".FixedLength(10) + "This will toggle the @echo -off.");
                        Print(string.Empty, ConsoleColor.Gray, true, true);
                        break;
                    default:
                        Print("Expected: <option(s)>".Push(2), ConsoleColor.Red, true, true);
                        break;
                }
            else
                DisplayCommands = !DisplayCommands;

        }

        private void CommandEcho(Output output)
        {

            int cIndex = output.GetStackIndex("echo");

            if (output.Commands[cIndex].Count > 1)
                switch (output.Commands[cIndex][1])
                {
                    case "$time":
                        Print(DateTime.Now.TimeOfDay.ToString(), ConsoleColor.White, true, true);
                        break;
                    case "$date":
                        Print(DateTime.Now.Date.ToString(), ConsoleColor.White, true, true);
                        break;
                    case "$now":
                        Print(DateTime.Now.ToString(), ConsoleColor.White, true, true);
                        break;
                    case "$year":
                        Print(DateTime.Now.Year.ToString(), ConsoleColor.White, true, true);
                        break;
                    case "$day":
                        Print(DateTime.Now.DayOfWeek.ToString(), ConsoleColor.White, true, true);
                        break;
                    case "$path":
                        Print(AppDomain.CurrentDomain.BaseDirectory, ConsoleColor.White, true, true);
                        break;

                    // more echo values.

                    default:
                        Print(output.Commands[cIndex][1].Replace("\"", string.Empty), ConsoleColor.White, true, true);
                        break;
                }
            else
                Print("Expected: <string>", ConsoleColor.Red, true, true);

        }

        private void CommandWait(Output output)
        {

            int cIndex = output.GetStackIndex("wait");

            if (output.Commands[cIndex].Count > 1)
            {

                if (!output.Commands[cIndex][1].IsNumeric())
                {
                    Print("Expected: <time>".Push(2), ConsoleColor.Red, true, true);
                    return;
                }

                output.WaitTime = Convert.ToInt32(output.Commands[cIndex][1]);

                ThreadManager.Add(new OThread(new ParameterizedThreadStart(e => {
                    Output output = (Output)e;
                    Thread.Sleep(output.WaitTime);
                    output.WaitTime = 0;
                    output.Commands.RemoveAt(cIndex);
                    OnRunOutput?.Invoke(output);
                }))).Start(output);

            }
            else
                Print("Expected: <time>", ConsoleColor.Red, true, true);

        }

        private void CommandClear(Output output)
        {
            PrintClear();
        }

        private void CommandExecute(Output output)
        {

            int cIndex = output.GetStackIndex("exec");

            if (output.Commands[cIndex].Count > 1)
                ExecuteConfigFile(output.Commands[cIndex][1]);
            else
                Print("Expected: <filename>".Push(2), ConsoleColor.Red, true, true);

        }

        private void CommandExit(Output output)
        {

            int cIndex = output.GetStackIndex("exit");

            if (output.Commands[cIndex].Count > 1)
                switch (output.Commands[cIndex][1])
                {
                    case "help":
                        Print(string.Empty);
                        Print("help: command \"exit\"");
                        Print(string.Empty);
                        Print("-y".Push(2).FixedLength(10) + "-".FixedLength(15) + "This will over ride the question and just quit.");
                        Print(string.Empty, ConsoleColor.White, true, true);
                        break;
                    case "-y":
                        Print(string.Empty);
                        Print("Raise Event: System Shut Down...".Push(2), ConsoleColor.Green, true);
                        Print(string.Empty);
                        ConsoleAction?.Invoke(OConsoleState.Shutdown);
                        break;
                    default:
                        Print("Expected: <option>".Push(2), ConsoleColor.Red, true, true);
                        break;
                }
            else
            {
                OnQuestion = e => {
                    if (e == OQuestionAnswer.Yes)
                    {
                        Print(string.Empty);
                        Print("Raise Event: System Shut Down...".Push(2), ConsoleColor.Green, true);
                        Print(string.Empty);
                        ConsoleAction?.Invoke(OConsoleState.Shutdown);
                    }
                    else if (e == OQuestionAnswer.No)
                        Print("exit command canceled.".Push(2), ConsoleColor.Red, true, true);
                };

                Print("Are you sure you want to exit the console?".Push(2), ConsoleColor.Yellow, true, true);
            }
        }

        private void CommandRestart(Output output)
        {
            int cIndex = output.GetStackIndex("restart");

            if (output.Commands[cIndex].Count > 1)
                switch (output.Commands[cIndex][1])
                {
                    case "help":
                        Print(string.Empty);
                        Print("help: command \"restart\"");
                        Print(string.Empty);
                        Print("-y".Push(2).FixedLength(10) + "-".FixedLength(15) + "This will override the question and just run.");
                        Print(string.Empty, ConsoleColor.White, true, true);
                        break;
                    case "-y":
                        Print(string.Empty);
                        Print("Raise Event: System Restarting...".Push(2), ConsoleColor.Green, true);
                        Print(string.Empty);
                        ConsoleAction?.Invoke(OConsoleState.Reboot);
                        break;
                    default:
                        Print("Expected: <option>".Push(2), ConsoleColor.Red, true, true);
                        break;
                }
            else
            {
                OnQuestion = e =>
                {
                    if (e == OQuestionAnswer.Yes)
                    {
                        Print(string.Empty);
                        Print("Raise Event: System Restarting...".Push(2), ConsoleColor.Green, true);
                        Print(string.Empty);
                        ConsoleAction?.Invoke(OConsoleState.Reboot);
                    }
                    else if (e == OQuestionAnswer.No)
                        Print("restart command canceled.".Push(2), ConsoleColor.Red, true, true);
                };

                Print("Are you sure you want to restart the console?".Push(2), ConsoleColor.Yellow, true, true);
            }

        }

        private void CommandShow(Output output)
        {
            
            OManagerCommands manager = (OManagerCommands)CommandManager.Values.First();

            int cIndex = output.GetStackIndex("show");

            if (output.Commands[cIndex].Count > 1)
                switch (output.Commands[cIndex][1])
                {
                    case "-v":
                        manager.ListMethodsX();
                        break;
                    case "-f":
                        manager.ListFunctionsX();
                        break;
                    case "-m":
                        CommandManager.Values.ForEach(m => {
                            Print("Listed by "+  m.GetType().Name);
                            Print(m.List(), ConsoleColor.Green);
                            Print("----------------------------------", ConsoleColor.White, true, true);
                        });
                        break;
                    case "help":
                        Print(string.Empty);
                        Print("help: command \"show\"");
                        Print(string.Empty);
                        Print("-m".Push(2).FixedLength(10) + "-".FixedLength(30) + "This will show all managers.");
                        Print("-v".Push(2).FixedLength(10) + "-".FixedLength(30) + "This will list methods code you can use with in the extentions manager.");
                        Print("-f".Push(2).FixedLength(10) + "-".FixedLength(30) + "This will list functions code you can use with in the extentions manager.");
                        Print(string.Empty, ConsoleColor.White, true, true);
                        break;
                    default:
                        Print("list: Invalid".Push(2), ConsoleColor.Red, true, true);
                        break;
                }
            else
                Print("Expected: <option(s)>".Push(2), ConsoleColor.Red, true, true);
        }

        private void CommandHelp(Output output)
        {
            PrintHelp();
        }

        private void CommandVersion(Output output)
        {
            PrintVersion();
        }
        
        private void CommandPad(Output output) 
        {
            
            int cIndex = output.GetStackIndex("pad");

            if (output.Commands[cIndex].Count > 1)
                switch (output.Commands[cIndex][1])
                {
                    case "-f":
                        if (output.Commands[cIndex].Count == 3)
                        {
                           
                            var file = output.Commands[cIndex][2].Replace("\"", string.Empty);

                            if (!File.Exists(EnvironmentPath + "\\" + file))
                                File.WriteAllLines(EnvironmentPath + "\\" + file, new string[] { "" });

                            TextEditor.LoadFile(EnvironmentPath + "\\" + file);
                            System.Console.Clear();
                            TextEditorEnabled = true;

                        }
                        else
                            Print("Expected: <FILENAME>".Push(2), ConsoleColor.Red, true, true);
                        break;
                    case "-v":
                        if (output.Commands[cIndex].Count == 3)
                        {
                            var name = output.Commands[cIndex][2].Replace("\"", string.Empty);
                            var manager = (OManagerVariables)CommandManager.Values.FirstOrDefault(t => t.GetType() == typeof(OManagerVariables));
                            if (manager != null)
                            {
                                if (manager.Components.ContainsKey(name))
                                {
                                    var m = (OManagerVariables.Variable)manager.Components[name];
                                    TextEditor.LoadContent(m.Value.ToString());
                                    TextEditor.SaveType = m;
                                    System.Console.Clear();
                                    TextEditorEnabled = true;
                                }
                                else 
                                    Print("Expected: variable does not exist.", ConsoleColor.Red, true, true);
                            }
                            else
                                Print("Expected: variable manager.", ConsoleColor.Red, true, true);
                        }
                        else
                            Print("Expected: <FILENAME>".Push(2), ConsoleColor.Red, true, true);
                        break;
                    case "-m":
                        if (output.Commands[cIndex].Count == 3)
                        {
                            var name = output.Commands[cIndex][2].Replace("\"", string.Empty);
                            var manager = (OManagerMacros)CommandManager.Values.FirstOrDefault(t => t.GetType() == typeof(OManagerMacros));
                            if (manager != null)
                            {
                                if (manager.Components.ContainsKey(name))
                                {
                                    var m = (OManagerMacros.Macro)manager.Components[name];
                                    TextEditor.LoadContent(m.Code.ToString());
                                    TextEditor.SaveType = m;
                                    System.Console.Clear();
                                    TextEditorEnabled = true;
                                }
                                else
                                    Print("Expected: macro does not exist.", ConsoleColor.Red, true, true);
                            }
                            else
                                Print("Expected: macro manager.", ConsoleColor.Red, true, true);
                        }
                        else
                            Print("Expected: <FILENAME>".Push(2), ConsoleColor.Red, true, true);
                        break;
                    case "help":
                        Print(string.Empty);
                        Print("help: command \"pad\"");
                        Print(string.Empty);
                        Print("-f".Push(2).FixedLength(10) + "<FILENAME>".FixedLength(15) + "Starts the editor with an external or new file.");
                        Print("-v".Push(2).FixedLength(10) + "<NAME>".FixedLength(15) + "Starts the editor with a stored variable.");
                        Print("-m".Push(2).FixedLength(10) + "<NAME>".FixedLength(15) + "Starts the editor with a stored macro.");
                        Print(string.Empty);
                        Print("help: inside the editor");
                        Print(string.Empty);
                        Print("Ctrl Q".Push(2).FixedLength(10) + "-".FixedLength(5) + "This will close the editor but not clear the content.");
                        Print("Ctrl U".Push(2).FixedLength(10) + "-".FixedLength(5) + "This will undo the last action.");
                        Print("Ctrl S".Push(2).FixedLength(10) + "-".FixedLength(5) + "This will close and save the content to the set context (var, macrom or file).");
                        Print("Ctrl K".Push(2).FixedLength(10) + "-".FixedLength(5) + "This removes the line the cursor currently resides.");
                        Print("Ctrl X".Push(2).FixedLength(10) + "-".FixedLength(5) + "This clears the buffer and resets the editor.");
                        Print(string.Empty, ConsoleColor.White, true, true);
                        break;
                    default:
                        Print("Expected: <option>".Push(2), ConsoleColor.Red, true, true);
                        break;
                }
            else
            {
                System.Console.Clear();
                TextEditorEnabled = true;
            }

        }

        private void CommandNew(Output output) 
        {

            int cIndex = output.GetStackIndex("nc");

            if (output.Commands[cIndex].Count > 1)
                switch (output.Commands[cIndex][1])
                {
                    case "help":
                        Print(string.Empty);
                        Print("help: command \"nc\"");
                        Print(string.Empty);
                        Print("-".Push(2).FixedLength(25) + "-".FixedLength(15) + "This command creates new process of the console based on the executable.");
                        Print("-c or --command".Push(2).FixedLength(25) + "-".FixedLength(15) + "This will add a command to the startuo args.");
                        Print("-r or --root".Push(2).FixedLength(25) + "-".FixedLength(15) + "This will run the console as administrator.");
                        Print("-u or --user".Push(2).FixedLength(25) + "-".FixedLength(15) + "This will run the console as a specific.");
                        Print(string.Empty, ConsoleColor.White, true, true);
                        break;
                    default:

                        //Gets the options in the command
                        var args        = string.Empty;
                        var userName    = string.Empty;
                        var asAdmin     = output.Commands[cIndex].FirstOrDefault(option => option == "-r" || option == "--root");
                        var asUser      = output.Commands[cIndex].FirstOrDefault(option => option == "-u" || option == "--user");
                        var withArgs    = output.Commands[cIndex].FirstOrDefault(option => option == "-c" || option == "--command");

                        if (!string.IsNullOrEmpty(withArgs))
                            args = output.Commands[cIndex][(output.Commands[cIndex].IndexOf(withArgs) + 1)].Replace("\"", string.Empty);
                       
                        if (!string.IsNullOrEmpty(asUser))
                            userName = output.Commands[cIndex][(output.Commands[cIndex].IndexOf(asUser) + 1)].Replace("\"", string.Empty);
                       
                        if (!string.IsNullOrEmpty(asAdmin))
                            userName = "Administrator";

                        ProcessStartInfo pi = new(AppDomain.CurrentDomain.BaseDirectory + "\\" + AppDomain.CurrentDomain.FriendlyName + ".exe")
                        {
                            UseShellExecute = true,
                            CreateNoWindow  = false,
                            WindowStyle     = ProcessWindowStyle.Normal
                        };

                        if (!string.IsNullOrEmpty(args))
                            args.Split(";", StringSplitOptions.RemoveEmptyEntries).ForEach(a => { pi.ArgumentList.Add(a); });

                        if (string.IsNullOrEmpty(asAdmin) && string.IsNullOrEmpty(asUser))
                        {
                            Process.Start(pi);
                            Print(string.Empty, ConsoleColor.White, true, true);
                        }
                        else 
                        {
                            //We must append the username to the builder first so we get it back when the pwd has been typed.
                            PasswordBuilder.Append(userName + " ");

                            OnPassword = (isValid, uname, pwd) => {
                                if (isValid)
                                {
                                    pi.UseShellExecute = true;
                                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                                    {
                                        if (pwd != uname)
                                        {
                                            pi.UseShellExecute      = false;
                                            pi.UserName             = uname;
                                            pi.Domain               = Environment.MachineName;
                                            pi.PasswordInClearText  = pwd;
                                        }
                                        pi.Verb = "runas";
                                    }
                                    pi.ArgumentList.Add("user " + (uname == "Administrator" ? "root" : uname));
                                    Process.Start(pi);
                                    Print(string.Empty, ConsoleColor.White, true, true);
                                }
                                else
                                {
                                    Print(string.Empty);
                                    Print("Password incorrect.".Push(2), ConsoleColor.Red, true, true);
                                }
                                PasswordKeys = false;
                            };
                            Print("Password:", ConsoleColor.Gray, false);
                            PasswordKeys = true;
                        }
                    break;
                }
            else
            {
                Process.Start(new ProcessStartInfo(AppDomain.CurrentDomain.BaseDirectory + "\\" + AppDomain.CurrentDomain.FriendlyName + ".exe") { 
                    UseShellExecute  = true,
                    CreateNoWindow   = false,
                    WindowStyle      = ProcessWindowStyle.Normal
                });
                Print(string.Empty, ConsoleColor.Gray, true, true);
            }

        }

        private void CommandWhoAmI(Output output) 
        {

            int cIndex = output.GetStackIndex("whoami");

            if (output.Commands[cIndex].Count > 1)
                switch (output.Commands[cIndex][1])
                {
                    case "help":
                        Print(string.Empty);
                        Print("help: command \"whoami\"");
                        Print(string.Empty);
                        Print("-".Push(2).FixedLength(10) + "-".FixedLength(15) + "This command displays the details of the current user.");
                        Print(string.Empty, ConsoleColor.White, true, true);
                        break;
                    default:
                        Print("Expected: <option>".Push(2), ConsoleColor.Red, true, true);
                        break;
                }
            else
            {                
                Print(Environment.UserDomainName + "\\" + Environment.UserName, ConsoleColor.Gray, true, true);
            }


        }
       
        private void CommandPath(Output output)
        {
            int cIndex = output.GetStackIndex("cd");

            if (output.Commands[cIndex].Count > 1)
                switch (output.Commands[cIndex][1])
                {
                    case "..": //Move the env dir back one dir
                        var currentpath = EnvironmentPath;
                        if (currentpath.EndsWith("\\"))
                            currentpath = currentpath.Remove(currentpath.Length - 1, 1);
                        currentpath = currentpath.Substring(0, currentpath.LastIndexOf("\\"));
                        if (Directory.Exists(currentpath))
                        {
                            EnvironmentPath = new DirectoryInfo(currentpath).FullName;
                            PrintPrefix(true);
                            Print(EnvironmentPath, ConsoleColor.White, true, true);
                        }
                        else
                            Print("Error: The path selected doesn't exist.", ConsoleColor.Red, true, true);
                        break;
                    case "-a":
                        Print("System Environment Paths", ConsoleColor.Green, true);
                        Enum.GetValues(typeof(Environment.SpecialFolder))
                            .Cast<Environment.SpecialFolder>()
                            .Select(specialFolder => new {
                                Name = specialFolder.ToString(),
                                Path = Environment.GetFolderPath(specialFolder)
                            })
                            .OrderBy(item => item.Path.ToLower())
                            .ForEach(item => {
                                Print(item.Name.FixedLength(30) + item.Path, ConsoleColor.Gray, true);
                            });
                        Print(string.Empty, ConsoleColor.White, true, true);
                        break;
                    case "help":
                        Print(string.Empty);
                        Print("help: command \"path\"");
                        Print(string.Empty);
                        Print("-a".Push(2).FixedLength(10) + "-".FixedLength(10) + "Returns the system environment paths on the computer.");
                        Print("..".Push(2).FixedLength(10) + "-".FixedLength(10) + "Sets the environment path on the terminal.");
                        Print("<name>".Push(2).FixedLength(10) + "-".FixedLength(10) + "Returns the system environment paths on the computer.");
                        Print(string.Empty, ConsoleColor.White, true, true);
                        break;
                    default: //Move the env dir forward one dir based on the name input
                        var subPath = output.Commands[cIndex][1].Replace("\"", string.Empty);
                        subPath = subPath.Replace("/", "\\");
                        if (EnvironmentPath.EndsWith("\\"))
                            EnvironmentPath = EnvironmentPath.Remove(EnvironmentPath.Length - 1, 1);
                        if (Directory.Exists(EnvironmentPath + "\\" + subPath))
                        {
                            EnvironmentPath = new DirectoryInfo(EnvironmentPath + "\\" + subPath).FullName;
                            PrintPrefix(true);
                            Print(EnvironmentPath, ConsoleColor.White, true, true);
                        }
                        else if (Directory.Exists(subPath))
                        {
                            EnvironmentPath = new DirectoryInfo(subPath).FullName;
                            PrintPrefix(true);
                            Print(EnvironmentPath, ConsoleColor.White, true, true);
                        }
                        else
                            Print("Error: The path selected doesn't exist.", ConsoleColor.Red, true, true);
                        break;
                }
            else
                Print("Environment path set: " + EnvironmentPath, ConsoleColor.Green, true, true);

        }

        private void CommandDir(Output output)
        {

            int cIndex = output.GetStackIndex("ls");

            if (output.Commands[cIndex].Count > 1)
                switch (output.Commands[cIndex][1])
                {
                    case "-at":

                        new DirectoryInfo(EnvironmentPath)
                            .GetDirectories()
                            .Where(d => !d.Attributes.HasFlag(FileAttributes.Hidden))
                            .ForEach(di => {
                                Print("\\" + di.Name.Ellipses(50).FixedLength(50), ConsoleColor.Gray, false);
                                Print(" [ ", ConsoleColor.Magenta, false);
                                Print(di.Attributes.ToString(), ConsoleColor.DarkGreen, false);
                                Print(" ]", ConsoleColor.Magenta, true);
                            });

                        new DirectoryInfo(EnvironmentPath)
                            .GetFiles()
                            .Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden))
                            .ForEach(fi => {
                                Print("\\" + fi.Name.Ellipses(50).FixedLength(50), ConsoleColor.Gray, false);
                                Print(" [ ", ConsoleColor.Magenta, false);
                                Print(fi.Attributes.ToString(), ConsoleColor.DarkGreen, false);
                                Print(" ]", ConsoleColor.Magenta, true);
                            });
                       
                        Print(string.Empty, ConsoleColor.Gray, true, true);
                       
                        break;
                    case "-l":
                       
                        new DirectoryInfo(EnvironmentPath)
                            .GetDirectories()
                            .Where(d => !d.Attributes.HasFlag(FileAttributes.Hidden))
                            .ForEach(di => {
                           
                                string info = string.Empty;
                           
                                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                                {
                                    foreach (FileSystemAccessRule fsar in di.GetAccessControl().GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount)))
                                        if (fsar.AccessControlType == AccessControlType.Allow && fsar.IdentityReference.Value == Environment.MachineName + "\\" + Environment.UserName) 
                                            info += "-" + fsar.FileSystemRights.Fsrm();
                                }
                           
                                info = info.FixedLength(15);
                                info += " " + Environment.UserName.FixedLength(10) + " " + di.GetSize().ToString().FixedLength(10) + " " + di.LastAccessTime.ToString("MMM dd HH:mm") + " ";
                           
                                Print(info + "/" + di.Name, ConsoleColor.Gray, true);

                            });
                       
                        new DirectoryInfo(EnvironmentPath)
                            .GetFiles()
                            .Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden))
                            .ForEach(fi => {
                               
                                string info = string.Empty;
                               
                                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                                {
                                    foreach (FileSystemAccessRule fsar in fi.GetAccessControl().GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount)))
                                        if (fsar.AccessControlType == AccessControlType.Allow && fsar.IdentityReference.Value == Environment.MachineName + "\\" + Environment.UserName)
                                            info += "-" + fsar.FileSystemRights.Fsrm();
                                }
                                
                                info = info.FixedLength(15);
                                info += " " + Environment.UserName.FixedLength(10) + " " + fi.Length.ToString().FixedLength(10) + " " + fi.LastWriteTime.ToString("MMM dd HH:mm") + " ";
                                
                                Print(info + "/" + fi.Name, ConsoleColor.Gray, true);
                          
                            });

                        Print(string.Empty, ConsoleColor.Gray, true, true);

                        break;
                    case "-a":

                        new DirectoryInfo(EnvironmentPath)
                            .GetDirectories()
                            .ForEach(di => {

                                string info = string.Empty;

                                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                                {
                                    foreach (FileSystemAccessRule fsar in di.GetAccessControl().GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount)))
                                        if (fsar.AccessControlType == AccessControlType.Allow && fsar.IdentityReference.Value == Environment.MachineName + "\\" + Environment.UserName)
                                            info += "-" + fsar.FileSystemRights.Fsrm();
                                }

                                info = info.FixedLength(15);
                                info += " " + Environment.UserName.FixedLength(10) + " " + di.GetSize().ToString().FixedLength(10) + " " + di.LastAccessTime.ToString("MMM dd HH:mm") + " ";

                                Print(info + "/" + di.Name, ConsoleColor.Gray, true);

                            });

                        new DirectoryInfo(EnvironmentPath)
                            .GetFiles()
                            .ForEach(fi => {

                                string info = string.Empty;

                                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                                {
                                    foreach (FileSystemAccessRule fsar in fi.GetAccessControl().GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount)))
                                        if (fsar.AccessControlType == AccessControlType.Allow && fsar.IdentityReference.Value == Environment.MachineName + "\\" + Environment.UserName)
                                            info += "-" + fsar.FileSystemRights.Fsrm();
                                }

                                info = info.FixedLength(15);
                                info += " " + Environment.UserName.FixedLength(10) + " " + fi.Length.ToString().FixedLength(10) + " " + fi.LastWriteTime.ToString("MMM dd HH:mm") + " ";

                                Print(info + "/" + fi.Name, ConsoleColor.Gray, true);

                            });

                        Print(string.Empty, ConsoleColor.Gray, true, true);

                        break;
                    case "-lh":

                        Print("total " + gl.ConvertBytes((ulong)new DirectoryInfo(EnvironmentPath).GetSize()), ConsoleColor.Gray, true);

                        new DirectoryInfo(EnvironmentPath)
                            .GetDirectories()
                            .Where(d => !d.Attributes.HasFlag(FileAttributes.Hidden))
                            .ForEach(di => {
                           
                                string info = string.Empty;
                           
                                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                                {
                                    foreach (FileSystemAccessRule fsar in di.GetAccessControl().GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount)))
                                        if (fsar.AccessControlType == AccessControlType.Allow && fsar.IdentityReference.Value == Environment.MachineName + "\\" + Environment.UserName) 
                                            info += "-" + fsar.FileSystemRights.Fsrm();
                                }
                           
                                info = info.FixedLength(15);
                                info += " " + Environment.UserName.FixedLength(10) + " " + gl.ConvertBytes((ulong)di.GetSize(), "k").FixedLength(10) + " " + di.LastAccessTime.ToString("MMM dd HH:mm") + " ";
                           
                                Print(info + "/" + di.Name, ConsoleColor.Gray, true);

                            });
                       
                        new DirectoryInfo(EnvironmentPath)
                            .GetFiles()
                            .Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden))
                            .ForEach(fi => {
                               
                                string info = string.Empty;
                               
                                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                                {
                                    foreach (FileSystemAccessRule fsar in fi.GetAccessControl().GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount)))
                                        if (fsar.AccessControlType == AccessControlType.Allow && fsar.IdentityReference.Value == Environment.MachineName + "\\" + Environment.UserName)
                                            info += "-" + fsar.FileSystemRights.Fsrm();
                                }
                                
                                info = info.FixedLength(15);
                                info += " " + Environment.UserName.FixedLength(10) + " " + gl.ConvertBytes((ulong)fi.Length, "k").FixedLength(10) + " " + fi.LastWriteTime.ToString("MMM dd HH:mm") + " ";
                                
                                Print(info + "/" + fi.Name, ConsoleColor.Gray, true);
                          
                            });

                        Print(string.Empty, ConsoleColor.Gray, true, true);

                        break;
                    case "-lS":

                        new DirectoryInfo(EnvironmentPath)
                            .GetFiles()
                            .OrderBy(f => f.Length)
                            .ForEach(fi => {

                                string info = string.Empty;

                                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                                {
                                    foreach (FileSystemAccessRule fsar in fi.GetAccessControl().GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount)))
                                        if (fsar.AccessControlType == AccessControlType.Allow && fsar.IdentityReference.Value == Environment.MachineName + "\\" + Environment.UserName)
                                            info += "-" + fsar.FileSystemRights.Fsrm();
                                }

                                info = info.FixedLength(15);
                                info += " " + Environment.UserName.FixedLength(10) + " " + gl.ConvertBytes((ulong)fi.Length, "k").FixedLength(10) + " " + fi.LastWriteTime.ToString("MMM dd HH:mm") + " ";

                                Print(info + "/" + fi.Name, ConsoleColor.Gray, true);

                            });

                        Print(string.Empty, ConsoleColor.Gray, true, true);

                        break;
                    case "-ltr":

                        new DirectoryInfo(EnvironmentPath)
                            .GetFiles()
                            .OrderBy(f => f.LastWriteTime)
                            .ForEach(fi => {

                                string info = string.Empty;

                                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                                {
                                    foreach (FileSystemAccessRule fsar in fi.GetAccessControl().GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount)))
                                        if (fsar.AccessControlType == AccessControlType.Allow && fsar.IdentityReference.Value == Environment.MachineName + "\\" + Environment.UserName)
                                            info += "-" + fsar.FileSystemRights.Fsrm();
                                }

                                info = info.FixedLength(15);
                                info += " " + Environment.UserName.FixedLength(10) + " " + gl.ConvertBytes((ulong)fi.Length, "k").FixedLength(10) + " " + fi.LastWriteTime.ToString("MMM dd HH:mm") + " ";

                                Print(info + "/" + fi.Name, ConsoleColor.Gray, true);

                            });

                        Print(string.Empty, ConsoleColor.Gray, true, true);

                        break;
                    case "help":
                        Print(string.Empty);
                        Print("help: command \"ls\"");
                        Print(string.Empty);
                        Print("-at".Push(2).FixedLength(10) + "-".FixedLength(10) + "Show all files and directories with their attributes.");
                        Print("-l".Push(2).FixedLength(10) + "-".FixedLength(10) + "Show all files and directories with the permissions.");
                        Print("-a".Push(2).FixedLength(10) + "-".FixedLength(10) + "Show all files and directories with the permissions including hidden.");
                        Print("-lh".Push(2).FixedLength(10) + "-".FixedLength(10) + "Show files and directories with the total size with the permissions.");
                        Print("-lS".Push(2).FixedLength(10) + "-".FixedLength(10) + "Show the biggest file last with the permissions.");
                        Print("-ltr".Push(2).FixedLength(10) + "-".FixedLength(10) + "Show the latest modification file date as last with the permissions.");
                        Print(string.Empty, ConsoleColor.White, true, true);
                        break;
                    default:
                        Print(string.Empty, ConsoleColor.Gray, true, true);
                        break;
                }
            else
                try
                {
                    Directory.GetDirectories(EnvironmentPath).ForEach(f => { Print(f, ConsoleColor.Gray, true); });
                    Directory.GetFiles(EnvironmentPath).ForEach(f => { Print(f, ConsoleColor.Gray, true); });
                    Print(string.Empty, ConsoleColor.Gray, true, true);
                }
                catch (Exception ex)
                {
                    Print("Error: ls:: " + ex.Message, ConsoleColor.Red, true, true);
                }

        }

        private void CommandMkDir(Output output) 
        {

            int cIndex = output.GetStackIndex("mkdir");

            if (output.Commands[cIndex].Count > 1)
                switch (output.Commands[cIndex][1])
                {
                    case "help":
                        Print(string.Empty);
                        Print("help: command \"mkdir\"");
                        Print(string.Empty);
                        Print("-p or --parents".Push(2).FixedLength(25) + "<PATH>".FixedLength(15) + "This command creates directoies recusivly or creates with in if already exists.");
                        Print("-v or --verbose".Push(2).FixedLength(25) + "-".FixedLength(15) + "This will print messages for each created directory.");
                        Print("-u or --user".Push(2).FixedLength(25) + "<USERNAME>".FixedLength(15) + "This will set the mode or security on the path to the user specified.");
                        Print("-m or --mode".Push(2).FixedLength(25) + "<OPTIONS>".FixedLength(15) + "This will set the permissions on all created directories in the path.");
                        Print(string.Empty);
                        Print("permissions: these can be built up in any combination, exmpale: rwx which means read write and execute");
                        Print(string.Empty);
                        Print("r".Push(2).FixedLength(10) + "mode option".FixedLength(15) + "Sets the read option.");
                        Print("w".Push(2).FixedLength(10) + "mode option".FixedLength(15) + "Sets the write option.");
                        Print("x".Push(2).FixedLength(10) + "mode option".FixedLength(15) + "Sets the execute option.");
                        Print("f".Push(2).FixedLength(10) + "mode option".FixedLength(15) + "Sets the full control option.");
                        Print("o".Push(2).FixedLength(10) + "mode option".FixedLength(15) + "Sets the take ownership option.");
                        Print("m".Push(2).FixedLength(10) + "mode option".FixedLength(15) + "Sets the modify option.");
                        Print(string.Empty, ConsoleColor.White, true, true);
                        break;
                    default:

                        //Gets the options in the command
                        var path            = string.Empty;
                        var mode            = string.Empty;
                        var user            = string.Empty;

                        var withParents     = output.Commands[cIndex].FirstOrDefault(option => option == "-p" || option == "--parents");
                        var withMessages    = output.Commands[cIndex].FirstOrDefault(option => option == "-v" || option == "--verbose");
                        var withMode        = output.Commands[cIndex].FirstOrDefault(option => option == "-m" || option == "--mode");
                        var withUser        = output.Commands[cIndex].FirstOrDefault(option => option == "-u" || option == "--user");

                        //Lets get the mode to set each dir with security
                        if (!string.IsNullOrEmpty(withMode))
                            mode = output.Commands[cIndex][(output.Commands[cIndex].IndexOf(withMode) + 1)].Replace("\"", string.Empty);

                        //Lets get the user if 
                        if (!string.IsNullOrEmpty(withUser))
                            user = output.Commands[cIndex][(output.Commands[cIndex].IndexOf(withUser) + 1)].Replace("\"", string.Empty);
                        else
                            user = Environment.UserName;

                        //Lets get the path and render as cd path
                        if (string.IsNullOrEmpty(withParents))
                            path = output.Commands[cIndex][1].Replace("\"", string.Empty).Replace("/", "\\");
                        else
                            path = output.Commands[cIndex][(output.Commands[cIndex].IndexOf(withParents) + 1)].Replace("\"", string.Empty).Replace("/", "\\");

                        if (!path.StartsWith("\\"))
                            path = "\\" + path;

                        //Lets get each dir from the path
                        string[] pathParts = path.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

                        //Lets validate the chars in the path
                        char[] inValid = new char[] { (char)92, (char)47, (char)58, (char)42, (char)63, (char)34, (char)60, (char)62, (char)46, (char)124 };

                        foreach (string part in pathParts)
                            if (part.ToCharArray().Any(x => inValid.Any(y => y == x)))
                            {
                                Print(@"The name cannot contain any of these chars  \ / : * ? " + ((char)34).ToString() + " < > | .".Push(2), ConsoleColor.Red, true, true);
                                return;
                            }                        

                        //Setup for the loop and creation.
                        int         pathPosition    = 0;
                        string      completePath    = EnvironmentPath;

                        do
                        {

                            completePath += "\\" + pathParts[pathPosition];

                            if (!Directory.Exists(completePath))
                            {
                                Directory.CreateDirectory(completePath);

                                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                                    if (!string.IsNullOrEmpty(mode))
                                    {
                                        DirectoryInfo       di          = new(completePath);
                                        DirectorySecurity   diSecurity  = di.GetAccessControl();

                                        di.Attributes.SetFlags(FileAttributes.Normal);
                                        di.Attributes.ClearFlags(FileAttributes.ReadOnly);

                                        if (mode.Contains("r"))
                                            diSecurity.AddAccessRule(new FileSystemAccessRule(user, FileSystemRights.Read, AccessControlType.Allow));

                                        if (mode.Contains("w"))
                                            diSecurity.AddAccessRule(new FileSystemAccessRule(user, FileSystemRights.Write, AccessControlType.Allow));

                                        if (mode.Contains("x"))
                                            diSecurity.AddAccessRule(new FileSystemAccessRule(user, FileSystemRights.ExecuteFile, AccessControlType.Allow));

                                        if (mode.Contains("f"))
                                            diSecurity.AddAccessRule(new FileSystemAccessRule(user, FileSystemRights.FullControl, AccessControlType.Allow));
                                       
                                        if (mode.Contains("o"))
                                            diSecurity.AddAccessRule(new FileSystemAccessRule(user, FileSystemRights.TakeOwnership, AccessControlType.Allow));
                                       
                                        if (mode.Contains("m"))
                                            diSecurity.AddAccessRule(new FileSystemAccessRule(user, FileSystemRights.Modify, AccessControlType.Allow));

                                        di.SetAccessControl(diSecurity);
                                        di = null;
                                    }

                                if (!string.IsNullOrEmpty(withMessages))
                                    Print("Created" + (string.IsNullOrEmpty(mode) ? " :" : " with " + mode + " :") + completePath, ConsoleColor.Yellow);

                            }

                            if (string.IsNullOrEmpty(withParents))
                                pathPosition = pathParts.Length;
                            else
                                pathPosition++;

                        } while (pathPosition != pathParts.Length);

                        Print(string.Empty, ConsoleColor.Gray, true, true);

                        break;
                }
            else
                Print("Expected: <option(s)>".Push(2), ConsoleColor.Red, true, true);

        }

        private void CommandCopy(Output output) 
        {

            int cIndex = output.GetStackIndex("cp");

            if (output.Commands[cIndex].Count > 1)
                switch (output.Commands[cIndex][1])
                {
                    case "help":
                        Print(string.Empty);
                        Print("help: command \"cp\"");
                        Print(string.Empty);
                        Print("-i".Push(2).FixedLength(10) + "-".FixedLength(30) + "This set the interactive option for if there is an overwrite occurance.");
                        Print("-r".Push(2).FixedLength(10) + "-".FixedLength(30) + "This set the recursive option for folders and files in folders.");
                        Print(string.Empty, ConsoleColor.White, true, true);
                        break;
                    default:

                        //Lets grab the options
                        bool interactive    = output.Commands[cIndex].Where(o => o == "-i").Any();
                        bool recursive      = output.Commands[cIndex].Where(o => o == "-r").Any();
                        bool overwrite      = false;

                        //Let remove the options from the stack
                        List<string> filtered = output.Commands[cIndex]
                            .ToArray()
                            .Filter(x => !x.StartsWith("-") && x != "cp")
                            .ToList();

                        //Lets get the destination
                        string destination = filtered.Last();
                        filtered.Remove(destination);

                        //Let get all the sources
                        string[] sources = filtered.ToArray();
                        filtered.Clear();

                        //Lets make sure there are sources to work with
                        if (sources != null && sources.Length > 0)
                        {

                            //Let auto prep the sources and destination.
                            destination = destination.Replace("\"", string.Empty).Replace("/", "\\");
                            if(!destination.StartsWith("\\") && !destination.Contains(":\\"))
                                destination = "\\" + destination;
                            if (!destination.Contains(":\\")) 
                                destination = EnvironmentPath + destination;

                            //We want to edit the contents of the list of sources.
                            for (var i = 0; i < sources.Length; i++) 
                            {
                                sources[i] = sources[i].Replace("\"", string.Empty).Replace("/", "\\");
                                if (!sources[i].StartsWith("\\") && !sources[i].Contains(":\\"))
                                    sources[i] = "\\" + sources[i];
                                if (!sources[i].Contains(":\\"))
                                    sources[i] = EnvironmentPath + sources[i];
                            }

                            void CpProcess(object e)
                            {

                                string[] destinationParts = (string[])e;

                                sources.ForEach(source =>
                                {
                                    if (File.GetAttributes(destination).HasFlag(FileAttributes.Directory))
                                    {
                                        string filter = "*.*";
                                        if (source.Contains("*."))
                                        {
                                            filter              = source.Remove(0, source.LastIndexOf("\\") + 1);
                                            DirectoryInfo mi    = new(EnvironmentPath);
                                            int fileNum         = 0;
                                            int fileCoutn       = mi.GetFiles("*.*", SearchOption.AllDirectories).Length;
                                            
                                            using var p = new OCommandProgressBar();
                                            mi.CopyTo(new DirectoryInfo(destination), (sourceFi) => 
                                                {
                                                    fileNum++;
                                                    p.Report((double)fileNum / fileCoutn);
                                                },
                                                recursive,
                                                overwrite,
                                                filter
                                            );

                                        }
                                        else
                                        {
                                            if (File.GetAttributes(source).HasFlag(FileAttributes.Directory))
                                            {
                                                DirectoryInfo mi    = new(source);
                                                int fileNum         = 0;
                                                int fileCoutn       = mi.GetFiles("*.*", SearchOption.AllDirectories).Length;
                                                
                                                using var p = new OCommandProgressBar();
                                                mi.CopyTo(new DirectoryInfo(destination), (sourceFi) =>
                                                    {
                                                        fileNum++;
                                                        p.Report((double)fileNum / fileCoutn);
                                                    },
                                                    recursive,
                                                    overwrite,
                                                    filter
                                                );
                                            }
                                            else
                                            {
                                                var fi = new FileInfo(source);
                                                fi.CopyTo(Path.Combine(destination, fi.Name), overwrite);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (!File.GetAttributes(source).HasFlag(FileAttributes.Directory))
                                        {
                                            //we are assuming there is only one source at this point.
                                            //If there is more than one source then the last source will be the one that is used.
                                            byte[] sourceData = File.ReadAllBytes(source);

                                            FileStream fi = File.Open(destination, FileMode.Truncate, FileAccess.ReadWrite, FileShare.ReadWrite);
                                            fi.Write(sourceData, 0, sourceData.Length);
                                            fi.Close();
                                            fi.Dispose();
                                        }
                                    }
                                });

                                Print(string.Empty, ConsoleColor.Gray, true, true);

                            }

                            //Lets determin the destination and create if needed.
                            bool IsFilePath = destination.IsFilePath(out string[] pathParts);

                            if (!IsFilePath)
                            {
                                if (!Directory.Exists(destination))
                                    Directory.CreateDirectory(destination);
                            }
                            else
                            {
                                if (!File.Exists(destination))
                                    File.WriteAllText(destination, string.Empty);
                                else
                                {
                                    if (interactive)
                                    {
                                        OnQuestion = e =>
                                        {
                                            if (e == OQuestionAnswer.Yes)
                                            {
                                                overwrite = true;
                                                CpProcess(pathParts);
                                            }
                                            else
                                                Print("Operation Canceled.", ConsoleColor.Gray, true, true);
                                        };
                                        Print("Are you sure you want to overwrite files?".Push(2), ConsoleColor.Yellow, true, true);
                                        return;
                                    }
                                }
                            }

                            if (!interactive)
                                CpProcess(pathParts);

                        }
                        else 
                            Print("Expected: source entries missing".Push(2), ConsoleColor.Red, true, true);
                        break;
                }
            else
                Print("Expected: <option(s)>".Push(2), ConsoleColor.Red, true, true);

        }

        private void CommandMove(Output output) 
        { 
        
            int cIndex = output.GetStackIndex("mv");

            if (output.Commands[cIndex].Count > 1)
                switch (output.Commands[cIndex][1])
                {
                    case "help":
                        Print(string.Empty);
                        Print("help: command \"mv\"");
                        Print(string.Empty);
                        Print("-i".Push(2).FixedLength(10) + "-".FixedLength(30) + "This set the interactive option for if there is an overwrite occurance.");
                        Print("-r".Push(2).FixedLength(10) + "-".FixedLength(30) + "This set the recursive option for folders and files in folders.");
                        Print(string.Empty, ConsoleColor.White, true, true);
                        break;
                    default:

                        //Lets grab the options
                        bool interactive    = output.Commands[cIndex].Where(o => o == "-i").Any();
                        bool recursive      = output.Commands[cIndex].Where(o => o == "-r").Any();
                        bool overwrite      = false;

                        //Let remove the options from the stack
                        List<string> filtered = output.Commands[cIndex]
                            .ToArray()
                            .Filter(x => !x.StartsWith("-") && x != "mv")
                            .ToList();

                        //Lets get the destination
                        string destination = filtered.Last();
                        filtered.Remove(destination);

                        //Let get all the sources
                        string[] sources = filtered.ToArray();
                        filtered.Clear();

                        //Lets make sure there are sources to work with
                        if (sources != null && sources.Length > 0)
                        {

                            //Let auto prep the sources and destination.
                            destination = destination.Replace("\"", string.Empty).Replace("/", "\\");
                            if(!destination.StartsWith("\\") && !destination.Contains(":\\"))
                                destination = "\\" + destination;
                            if (!destination.Contains(":\\")) 
                                destination = EnvironmentPath + destination;

                            //We want to edit the contents of the list of sources.
                            for (var i = 0; i < sources.Length; i++) 
                            {
                                sources[i] = sources[i].Replace("\"", string.Empty).Replace("/", "\\");
                                if (!sources[i].StartsWith("\\") && !sources[i].Contains(":\\"))
                                    sources[i] = "\\" + sources[i];
                                if (!sources[i].Contains(":\\"))
                                    sources[i] = EnvironmentPath + sources[i];
                            }

                            void MvProcess(object e)
                            {

                                string[] destinationParts = (string[])e;

                                sources.ForEach(source =>
                                {

                                    if (File.GetAttributes(destination).HasFlag(FileAttributes.Directory))
                                    {
                                        string filter = "*.*";
                                        if (source.Contains("*."))
                                        {
                                            filter              = source.Remove(0, source.LastIndexOf("\\") + 1);
                                            DirectoryInfo mi    = new(EnvironmentPath);
                                            int fileNum         = 0;
                                            int fileCoutn       = mi.GetFiles("*.*", SearchOption.AllDirectories).Length;
                                            
                                            using var p = new OCommandProgressBar();
                                            mi.CopyTo(new DirectoryInfo(destination), (sourceFi) => 
                                                {
                                                    fileNum++;
                                                    p.Report((double)fileNum / fileCoutn);
                                                    sourceFi.Delete();
                                                },
                                                recursive,
                                                overwrite,
                                                filter
                                            );

                                        }
                                        else
                                        {
                                            if (File.GetAttributes(source).HasFlag(FileAttributes.Directory))
                                            {
                                                DirectoryInfo mi    = new(source);
                                                int fileNum         = 0;
                                                int fileCoutn       = mi.GetFiles("*.*", SearchOption.AllDirectories).Length;
                                                
                                                using var p = new OCommandProgressBar();
                                                mi.CopyTo(new DirectoryInfo(destination), (sourceFi) =>
                                                    {
                                                        fileNum++;
                                                        p.Report((double)fileNum / fileCoutn); 
                                                        sourceFi.Delete();
                                                    },
                                                    recursive,
                                                    overwrite,
                                                    filter
                                                );
                                            }
                                            else
                                            {
                                                var fi = new FileInfo(source);
                                                fi.CopyTo(Path.Combine(destination, fi.Name), overwrite);
                                                fi.Delete();
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (!File.GetAttributes(source).HasFlag(FileAttributes.Directory))
                                        {
                                            //we are assuming there is only one source at this point.
                                            //If there is more than one source then the last source will be the one that is used.
                                            byte[] sourceData = File.ReadAllBytes(source);

                                            FileStream fi = File.Open(destination, FileMode.Truncate, FileAccess.ReadWrite, FileShare.ReadWrite);
                                            fi.Write(sourceData, 0, sourceData.Length);
                                            fi.Close();
                                            fi.Dispose();

                                            File.Delete(source);

                                        }
                                    }

                                    try
                                    {
                                        if (File.GetAttributes(source).HasFlag(FileAttributes.Directory))
                                            if (new DirectoryInfo(source).GetFiles("*.*", SearchOption.AllDirectories).Length <= 0)
                                                Directory.Delete(source, true);
                                    }
                                    catch { }

                                });

                                Print(string.Empty, ConsoleColor.Gray, true, true);

                            }

                            //Lets determin the destination and create if needed.
                            bool IsFilePath = destination.IsFilePath(out string[] pathParts);

                            if (!IsFilePath)
                            {
                                if (!Directory.Exists(destination))
                                    Directory.CreateDirectory(destination);
                            }
                            else
                            {
                                if (!File.Exists(destination))
                                    File.WriteAllText(destination, string.Empty);
                                else
                                {
                                    if (interactive)
                                    {
                                        OnQuestion = e =>
                                        {
                                            if (e == OQuestionAnswer.Yes)
                                            {
                                                overwrite = true;
                                                MvProcess(pathParts);
                                            }
                                            else
                                                Print("Operation Canceled.", ConsoleColor.Gray, true, true);
                                        };
                                        Print("Are you sure you want to overwrite files?".Push(2), ConsoleColor.Yellow, true, true);
                                        return;
                                    }
                                }
                            }

                            if (!interactive)
                                MvProcess(pathParts);

                        }
                        else 
                            Print("Expected: source entries missing".Push(2), ConsoleColor.Red, true, true);
                        break;
                }
            else
                Print("Expected: <option(s)>".Push(2), ConsoleColor.Red, true, true);

        }

        private void CommandRemove(Output output) 
        {
            
            int cIndex = output.GetStackIndex("rm");

            if (output.Commands[cIndex].Count > 1)
                switch (output.Commands[cIndex][1])
                {
                    case "help":
                        Print(string.Empty);
                        Print("help: command \"rm\"");
                        Print(string.Empty);
                        Print("-i".Push(2).FixedLength(10) + "-".FixedLength(30) + "This set the interactive option for ask before deleting.");
                        Print("-r".Push(2).FixedLength(10) + "-".FixedLength(30) + "This set the recursive option for folders and files in folders.");
                        Print(string.Empty, ConsoleColor.White, true, true);
                        break;
                    default:

                        //Lets grab the options
                        bool interactive    = output.Commands[cIndex].Where(option => option == "-i" || option == "--interactive").Any();
                        bool recursive      = output.Commands[cIndex].Where(option => option == "-r" || option == "--recursive").Any();
                        bool force          = output.Commands[cIndex].Where(option => option == "-f" || option == "--force").Any();

                        //Let remove the options from the stack
                        List<string> filtered = output.Commands[cIndex]
                            .ToArray()
                            .Filter(x => !x.StartsWith("-") && x != "rm")
                            .ToList();

                        //Let get all the sources
                        string[] sources = filtered.ToArray();
                        filtered.Clear();

                        //Lets make sure there are sources to work with
                        if (sources != null && sources.Length > 0)
                        {

                            //Let auto prep the sources and destination. We want to edit the contents of the list of sources.
                            for (var i = 0; i < sources.Length; i++)
                            {
                                sources[i] = sources[i].Replace("\"", string.Empty).Replace("/", "\\");
                                if (!sources[i].StartsWith("\\") && !sources[i].Contains(":\\"))
                                    sources[i] = "\\" + sources[i];
                                if (!sources[i].Contains(":\\"))
                                    sources[i] = EnvironmentPath + sources[i];
                            }

                            void RmProcessDelete(string source)
                            {

                                if(!File.Exists(source))
                                    return;

                                if (interactive)
                                {
                                    OnQuestion = e =>
                                    {
                                        if (e == OQuestionAnswer.Yes)
                                            File.Delete(source); 
                                        else
                                            Print("Operation Canceled.", ConsoleColor.Gray, true, true);
                                    };
                                    Print("Are you sure you want to delete file: ".Push(2) + new FileInfo(source).Name, ConsoleColor.Yellow, true, true);
                                    AwaitQuestionAnswer();
                                    return;
                                }
                                else
                                    File.Delete(source);

                            }

                            sources.ForEach(source =>
                            {
                                
                                string filter = string.Empty;
                               
                                if (source.Contains("\\*"))
                                {
                                    filter = source.Remove(0, source.LastIndexOf("\\") + 1);
                                    source = source.Remove(source.LastIndexOf("\\"));
                                }

                                if (File.GetAttributes(source).HasFlag(FileAttributes.Directory))
                                {
                                    if (!string.IsNullOrEmpty(filter))
                                    {
                                        DirectoryInfo mi = new(source);
                                        FileInfo[] fis = mi.GetFiles(filter, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                                        for (var x = 0; x < fis.Length; x++)
                                            RmProcessDelete(fis[x].FullName);
                                    }
                                    else
                                    {
                                        if (File.GetAttributes(source).HasFlag(FileAttributes.Directory))
                                        {
                                            DirectoryInfo   mi  = new(source);
                                            FileInfo[]      fis = mi.GetFiles("*.*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                                            for (var x = 0; x < fis.Length; x++) 
                                                RmProcessDelete(fis[x].FullName);
                                        }
                                        else
                                            RmProcessDelete(source);
                                    }
                                }
                                
                                if (!File.GetAttributes(source).HasFlag(FileAttributes.Directory))
                                    RmProcessDelete(source);

                                try
                                {
                                    if (File.GetAttributes(source).HasFlag(FileAttributes.Directory))
                                        if (new DirectoryInfo(source).GetFiles("*.*", SearchOption.AllDirectories).Length <= 0)
                                            Directory.Delete(source, recursive);
                                }
                                catch { }

                            });

                            Print(string.Empty, ConsoleColor.Gray, true, true);

                        }
                        else
                            Print("Expected: source entries missing".Push(2), ConsoleColor.Red, true, true);
                        break;
                }
            else
                Print("Expected: <option(s)>".Push(2), ConsoleColor.Red, true, true);

        }

        private bool CommandNotFound(Output output)
        {

            var c = CommandManager.Values.FirstOrDefault(c => c.Components.ContainsKey(output.Commands[0][0]));

            if (c != null)
            {
                c.Run(output.Commands[0][0], output, out object obj);

                if(obj != null)
                    Print(output.Commands[0][0] + " returned\r\n \"" + obj.ToString() + "\"", ConsoleColor.Yellow, true, true);

                return false;
            }

            return true;

        }

        public void ConfigurationSave()
        {
            try
            {

                ConfigWatcher.EnableRaisingEvents = false;

                JObject Configuration = new();

                CommandManager.Values.ForEach(c => {
                    if(c.GetType() != typeof(OManagerCommands))
                        Configuration.Add(new JProperty(c.GetType().Name, c.SaveConfiguration()));                
                });

                byte[] data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Configuration));

                if (File.Exists(ConfigFile))
                {
                    FileStream fs = new(ConfigFile, FileMode.Truncate, FileAccess.Write, FileShare.ReadWrite);
                    fs.Seek(0, SeekOrigin.Begin);
                    fs.Write(data, 0, data.Length);
                    fs.Close();
                    fs.Dispose();
                }
                else
                    File.WriteAllBytes(ConfigFile, data);

                ConfigWatcher.EnableRaisingEvents = true;

            }
            catch { }
        }

        public void ConfigurationLoad()
        {

            ConfigWatcher.EnableRaisingEvents = false;

            if (!File.Exists(ConfigFile))
                return;

            FileStream fs = new(ConfigFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            fs.Seek(0, SeekOrigin.Begin);
            byte[] data = new byte[fs.Length];
            fs.Read(data, 0, data.Length);
            fs.Close();
            fs.Dispose();

            JObject Configuration = JObject.Parse(Encoding.UTF8.GetString(data));

            CommandManager.Values.ForEach(c => {
                try
                {
                    if (c.GetType() != typeof(OManagerCommands))
                    {
                        Print(new string("Manager: " + c.Cmd + ".").FixedLength(35), ConsoleColor.Gray, false);
                        var prop = Configuration.Properties().SingleOrDefault(p => p.Name == c.GetType().Name);
                        if (prop != null) {
                            c.LoadConfiguration(prop.Value.ToString());
                            PrintStatus(OConsoleStatus.Ok); 
                        }
                    }
                }
                catch 
                {
                    PrintStatus(OConsoleStatus.Failed);
                }
            });

            ConfigWatcher.EnableRaisingEvents = true;

            Print(string.Empty, ConsoleColor.White, true, true);

        }
        
        #endregion

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

                CommandManager.Values.ForEach(v => {
                    v.OnPrint = null;
                    v.Dispose();
                });

                CommandManager.Clear();
            }

            IsDisposed = true;
        }

        #endregion

    }

}
