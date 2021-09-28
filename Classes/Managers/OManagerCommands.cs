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
using System.Reflection;
using System.Data;

using Microsoft.VisualBasic;

using K2host.Core;
using K2host.Data.Classes;
using K2host.Data.Enums;
using K2host.Console.Delegates;
using K2host.Console.Interfaces;
using K2host.Console.Classes;
using K2host.Console.Enums;

using gl = K2host.Core.OHelpers;
using gd = K2host.Data.OHelpers;

namespace K2host.Console.Managers
{


    public class OManagerCommands : IConsoleCommand
    {

        #region "Embedded IConsoleComponent"

        public class Command : IConsoleComponent
        {

            public string CommandKey { get; set; } = string.Empty;

            public string Description { get; set; } = string.Empty;

            public string SyntaxHelp { get; set; } = string.Empty;

            CallbackAsCommand ActionCommand = null;

            CallbackAsFunction ActionFunction = null;

            MethodInfo Method = null;

            readonly object Obj = null;

            public Command(string keyword, string action, object e)
            {
                CommandKey = keyword;

                Obj = e;

                if (Obj == null)
                    Method = typeof(OManagerCommands).GetMethod(action, BindingFlags.NonPublic | BindingFlags.Static);
                else 
                    Method = Obj.GetType().GetMethod(action, BindingFlags.Public | BindingFlags.Instance);

            }

            public Command(string keyword, CallbackAsCommand action)
            {
                CommandKey = keyword;
                ActionCommand = action;
            }

            public Command(string keyword, CallbackAsFunction action)
            {
                CommandKey = keyword;
                ActionFunction = action;
            }

            public object Run(Output output)
            {

                object result = null;

                if (ActionCommand != null)
                    ActionCommand.Invoke(output);

                if (ActionFunction != null)
                    return ActionFunction.Invoke(output);

                if (Method != null)
                    result = Method.Invoke(Obj, new object[] { output });

                return result;

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
                    Method = null;
                    ActionCommand = null;
                    ActionFunction = null;
                }

                IsDisposed = true;
            }

            #endregion

        }

        #endregion

        #region "Properties"

        public IConsole Parent { get; set; }

        public OnPrintEventHandler OnPrint { get; set; }

        public string Cmd { get; set; } = "command";

        public string HelpSyntax { get; set; } = "<OPTION(S)>";

        public string Description { get; set; } = "This command manager allows the registration of callbacks to action or functional code.";

        public Dictionary<string, IConsoleComponent> Components { get; } = new();

        #endregion

        #region "Constructor"

        public OManagerCommands() { }

        #endregion

        #region "Methods"

        public bool Add(string key, CallbackAsCommand action, string syntaxHelp, string description)
        {
            if (string.IsNullOrEmpty(key.Trim()))
            {
                OnPrint?.Invoke("Command register : Key is empty.".Push(2), ConsoleColor.Red, true, true);
                return false;
            }

            if (action == null)
            {
                OnPrint?.Invoke("Command register : Call back is empty.".Push(2), ConsoleColor.Red, true, true);
                return false;
            }

            Components.Add(key, new Command(key, action)
            {
                SyntaxHelp = syntaxHelp,
                Description = description
            });

            OnPrint?.Invoke("Command ".Push(2) + key + " Registered!", ConsoleColor.Green, true);

            return true;
        }

        public bool Add(string key, CallbackAsFunction action, string syntaxHelp, string description)
        {
            if (string.IsNullOrEmpty(key.Trim()))
            {
                OnPrint?.Invoke("Function register : Key is empty.".Push(2), ConsoleColor.Red, true, true);
                return false;
            }

            if (action == null)
            {
                OnPrint?.Invoke("Function register : Call back is empty.".Push(2), ConsoleColor.Red, true, true);
                return false;
            }

            Components.Add(key, new Command(key, action)
            {
                SyntaxHelp = syntaxHelp,
                Description = description
            });

            OnPrint?.Invoke("Function ".Push(2) + key + " Registered!", ConsoleColor.Green, true);

            return true;
        }

        public bool Add(string key, string action, object e, string syntaxHelp, string description)
        {

            if (string.IsNullOrEmpty(key.Trim()))
            {
                OnPrint?.Invoke("Command register : Key is empty.".Push(2), ConsoleColor.Red, true, true);
                return false;
            }

            if (string.IsNullOrEmpty(action.Trim()))
            {
                OnPrint?.Invoke("Command register : Call back is empty.".Push(2), ConsoleColor.Red, true, true);
                return false;
            }

            Components.Add(key, new Command(key, action, e)
            {
                SyntaxHelp = syntaxHelp,
                Description = description
            });

            OnPrint?.Invoke("Command ".Push(2) + key + " Registered!", ConsoleColor.Green, true);

            return true;
        }

        public void RemoveAll()
        {
            string ret = string.Empty;

            Components.Values.ForEach(c => {
                ret += "Command remove : " + c.CommandKey + " removed!\r\n";
                c.Dispose();
            });

            Components.Clear();

            OnPrint?.Invoke(ret, ConsoleColor.White, true, true);

        }

        public bool Remove(string key)
        {

            if (string.IsNullOrEmpty(key.Trim()))
            {
                OnPrint?.Invoke("Command remove : Key is empty.".Push(2), ConsoleColor.Red, true, true);
                return false;
            }

            if (!Components.ContainsKey(key))
            {
                OnPrint?.Invoke("Command remove : Key does not exist!".Push(2), ConsoleColor.Red, true, true);
                return false;
            }


            Components[key]?.Dispose();

            Components[key] = null;

            Components.Remove(key);

            OnPrint?.Invoke("Command remove : ".Push(2) + key + " removed!", ConsoleColor.Green, true, true);

            return true;
        }

        #endregion

        #region "Interfaced Methods"

        public string List()
        {
            string ret = string.Empty;

            Components.Values.ForEach(c => {
                ret += c.CommandKey.Push(2).FixedLength(20) + c.SyntaxHelp.FixedLength(40) + c.Description + "\r\n";
            });

            return ret;
        }

        public bool Run(string key, Output output, out object obj)
        {

            obj = null;

            if (string.IsNullOrEmpty(key.Trim()))
            {
                OnPrint?.Invoke("Command run : Key is empty.".Push(2), ConsoleColor.Red, true, true);
                return false;
            }

            if (output.Commands.Count <= 0)
            {
                OnPrint?.Invoke("Command run : Command is empty.".Push(2), ConsoleColor.Red, true, true);
                return false;
            }

            if (!Components.ContainsKey(key))
            {
                OnPrint?.Invoke("Command run : Key does not exist!".Push(2), ConsoleColor.Red, true, true);
                return false;
            }

            obj = ((Command)Components[key])?.Run(output);

            return true;

        }

        public void Parse(Output e)
        {

            List<string> subcommand = e.GetStack(Cmd);

            if (subcommand.Count > 1)
                switch (subcommand[1])
                {
                    case "help":
                        OnPrint?.Invoke(string.Empty);
                        OnPrint?.Invoke("help: command \"" + Cmd + "\"", ConsoleColor.Gray, true);
                        OnPrint?.Invoke(string.Empty);
                        OnPrint?.Invoke("-a or --add".Push(2).FixedLength(20) + "<NAME> <COMMAND> <SYNTAX> <DESCRIPTION>".FixedLength(48) + "This will register a command from an internal extention.");
                        OnPrint?.Invoke("-r or --remove".Push(2).FixedLength(20) + "<NAME>".FixedLength(48) + "This will remove a registered command from within the console.");
                        OnPrint?.Invoke("-l or --list".Push(2).FixedLength(20) + "-".FixedLength(48) + "This will list the registerd commands within the console.");
                        OnPrint?.Invoke(string.Empty, ConsoleColor.White, true, true);
                        break;
                    case "--add":
                    case "-a":
                        if (subcommand.Count >= 6)
                        {

                            object[] tmp = new object[] { null, null, null, null, null };

                            tmp[1] = subcommand[2].Replace("\"", string.Empty);     // Command Name
                            tmp[2] = subcommand[3].Replace("\"", string.Empty);     // Command Method Name
                            tmp[3] = subcommand[4].Replace("\"", string.Empty);     // Command syntax
                            tmp[4] = subcommand[5].Replace("\"", string.Empty);     // Command description

                            if (tmp[2].ToString() == string.Empty)
                            {
                                OnPrint?.Invoke("Error: Method is empty!".Push(2), ConsoleColor.Yellow, true, true);
                                return;
                            }

                            if (!MethodExists(tmp[2].ToString()) && !FunctionExists(tmp[2].ToString()))
                            {
                                OnPrint?.Invoke("Error: Method or Function does not exist!".Push(2), ConsoleColor.Yellow, true, true);
                                return;
                            }

                            Add(tmp[1].ToString(), tmp[2].ToString(), this, tmp[3].ToString(), tmp[4].ToString());
                            OnPrint?.Invoke(string.Empty, ConsoleColor.White, true, true);
                        }
                        else
                            OnPrint?.Invoke("Expected: <name> <code>".Push(2), ConsoleColor.Red, true, true);
                        break;
                    case "--remove":
                    case "-r":
                        if (subcommand.Count == 3)
                        {
                            Remove(subcommand[2].Replace("\"", string.Empty));
                            OnPrint?.Invoke(string.Empty, ConsoleColor.White, true, true);
                        }
                        else
                            OnPrint?.Invoke("Expected: <name>".Push(2), ConsoleColor.Red, true, true);
                        break;
                    case "--list":
                    case "-l":
                        OnPrint?.Invoke(string.Empty);
                        OnPrint?.Invoke("Listed Commands...", ConsoleColor.Yellow, true);
                        OnPrint?.Invoke("=".Strech(80), ConsoleColor.Gray, true);
                        OnPrint?.Invoke(List(), ConsoleColor.Green, true, true);
                        break;

                    default:
                        OnPrint?.Invoke("Expected <option(s)>".Push(2), ConsoleColor.Red, true, true);
                        break;
                }
            else
                OnPrint?.Invoke("Expected <option(s)>".Push(2), ConsoleColor.Red, true, true);


        }

        public void LoadConfiguration(string json) { }

        public string SaveConfiguration() => string.Empty;

        #endregion

        #region "Command Extentions"

        public bool MethodExists(string name)
        {

            var m = this.GetType()
                .GetMethods()
                .FirstOrDefault(m => m.Name.ToLower() == name.ToLower() && m.ReturnType.Name == "Void" && m.GetCustomAttribute<OCommandExention>() != null);

            if (m != null)
                return true;

            return false;

        }

        public bool FunctionExists(string name)
        {

            var m = this.GetType()
                .GetMethods()
                .FirstOrDefault(m => m.Name.ToLower() == name.ToLower() && m.ReturnType.Name != "Void" && m.GetCustomAttribute<OCommandExention>() != null);

            if (m != null)
                return true;

            return false;
        }

        public void ListFunctionsX()
        {
            OnPrint?.Invoke(string.Empty);

            //              |4   |25                       |20                  |
            OnPrint?.Invoke("#IDX|#Name                    |#Return type        |#Parameters");
            OnPrint?.Invoke("---------------------------------------------------------------------------");
            int j = 0;

            this.GetType()
                .GetMethods()
                .Where(m => m.GetCustomAttribute<OCommandExention>() != null && m.GetCustomAttribute<OCommandExention>().ExentionType == OConsoleExentionType.Funtion)
                .ForEach(m =>
                {

                    string Line = j.ToString();

                    if (j.ToString().Length > 4)
                        Line = Strings.Left(j.ToString(), 2) + ". |";
                    else
                        Line = j.ToString() + Strings.Space(4 - j.ToString().Length) + "|";

                    if (m.Name.Trim().Length > 25)
                        Line += Strings.Left(m.Name.Trim(), 21) + "... |";
                    else
                        Line += m.Name.Trim() + Strings.Space(25 - m.Name.Trim().Length) + "|";

                    if (m.ReturnType.Name.Length > 20)
                        Line += Strings.Left(m.ReturnType.Name, 16) + "... |";
                    else
                        Line += m.ReturnType.Name + Strings.Space(20 - m.ReturnType.Name.Length) + "|";

                    Line += m.GetCustomAttribute<OCommandExention>().Description;

                    OnPrint?.Invoke(Line);

                    j += 1;

                });

            OnPrint?.Invoke("    |                         |                    |");
            OnPrint?.Invoke("---------------------------------------------------------------------------");
            OnPrint?.Invoke(" " + j + " Function(s) found.");
            OnPrint?.Invoke(string.Empty, ConsoleColor.White, true, true);

        }

        public void ListMethodsX()
        {

            OnPrint?.Invoke(string.Empty);

            //              |4   |25                       |
            OnPrint?.Invoke("#IDX|#Name                    |#Parameters");
            OnPrint?.Invoke("-----------------------------------------------------------");
            int j = 0;

            this.GetType()
                .GetMethods()
                .Where(m => m.GetCustomAttribute<OCommandExention>() != null && m.GetCustomAttribute<OCommandExention>().ExentionType == OConsoleExentionType.Method)
                .ForEach(m =>
                {

                    string Line = j.ToString();

                    if (j.ToString().Length > 4)
                        Line = Strings.Left(j.ToString(), 2) + ". |";
                    else
                        Line = j.ToString() + Strings.Space(4 - j.ToString().Length) + "|";

                    if (m.Name.Trim().Length > 25)
                        Line += Strings.Left(m.Name.Trim(), 21) + "... |";
                    else
                        Line += m.Name.Trim() + Strings.Space(25 - m.Name.Trim().Length) + "|";

                    Line += m.GetCustomAttribute<OCommandExention>().Description;

                    OnPrint?.Invoke(Line);

                    j += 1;

                });

            OnPrint?.Invoke("    |                         |");
            OnPrint?.Invoke("-----------------------------------------------------------");
            OnPrint?.Invoke(" " + j + " Methods(s) found.");
            OnPrint?.Invoke(string.Empty, ConsoleColor.White, true, true);

        }

        //Decrypt from b64
        [OCommandExention(OConsoleExentionType.Funtion, "<Value(String)>")]
        public string DecryptB64(Output output)
        {

            try
            {

                string strResult = string.Empty;

                if (output.Commands[0].Count >= 1)
                    strResult = gl.DecryptB64(output.Commands[0][1].Replace("\"", string.Empty));
                else
                {
                    OnPrint?.Invoke("Too few parameters for Decrypt Base64!".Push(2), ConsoleColor.Yellow, true, true);
                    return string.Empty;
                }

                return strResult;

            }
            catch (Exception ex)
            {
                OnPrint?.Invoke("ERROR: ".Push(2) + ex.Message, ConsoleColor.Red, true, true);
                return string.Empty;
            }

        }

        //Encrypt from b64
        [OCommandExention(OConsoleExentionType.Funtion, "<Value(String)>")]
        public string EncryptB64(Output output)
        {

            try
            {

                string strResult = string.Empty;

                if (output.Commands[0].Count >= 1)
                    strResult = gl.EncryptB64(output.Commands[0][1].Replace("\"", string.Empty));
                else
                {
                    OnPrint?.Invoke("Too few parameters for Encrypt Base64!".Push(2), ConsoleColor.Yellow, true, true);
                    return string.Empty;
                }

                return strResult;

            }
            catch (Exception ex)
            {
                OnPrint?.Invoke("ERROR: ".Push(2) + ex.Message, ConsoleColor.Red, true, true);
                return string.Empty;
            }

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


            }

            IsDisposed = true;
        }
      
        #endregion

    }

}
