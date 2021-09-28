/*
' /====================================================\
'| Developed Tony N. Hyde (www.k2host.co.uk)            |
'| Projected Started: 2020-03-16                        | 
'| Use: General                                         |
' \====================================================/
*/

using System;
using System.Linq;
using System.Collections.Generic;

using Newtonsoft.Json;

using K2host.Core;
using K2host.Console.Classes;
using K2host.Console.Delegates;
using K2host.Console.Interfaces;

namespace K2host.Console.Managers
{


    public class OManagerAlias : IConsoleCommand
    {

        #region "Embedded IConsoleComponent"

        public class Alias : IConsoleComponent
        {

            public string CommandKey { get; set; } = string.Empty;

            public string Description { get; set; } = string.Empty;

            public string SyntaxHelp { get; set; } = string.Empty;

            public string Commands { get; set; } = string.Empty;
           
            public bool IsProtected { get; set; } = false;

            public Alias() { }

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

        #endregion

        #region "Properties"

        public IConsole Parent { get; set; }

        public OnPrintEventHandler OnPrint { get; set; }

        public string Cmd { get; set; } = "alias";

        public string HelpSyntax { get; set; } = "<OPTION(S)>";

        public string Description { get; set; } = "This alias manager allows you to store a short cut name with a value command.";

        public Dictionary<string, IConsoleComponent> Components { get; private set; } = new();

        #endregion

        #region "Constructor"

        public OManagerAlias() { }

        #endregion

        #region "Methods"

        public bool Add(string Name, string Command, bool IsProtected = false) 
        {
            if (string.IsNullOrEmpty(Name.Trim()))
            {
                OnPrint?.Invoke("Alias register : Name is empty.".Push(2), ConsoleColor.Red, true, true);
                return false;
            }

            if (string.IsNullOrEmpty(Command.Trim()))
            {
                OnPrint?.Invoke("Alias register : Command is empty.".Push(2), ConsoleColor.Red, true, true);
                return false;
            }

            Components.Add(Name, new Alias()
            {
                CommandKey  = Name,
                Commands    = Command,
                IsProtected = IsProtected
            });

            Parent.ConfigurationSave();

            OnPrint?.Invoke("Alias ".Push(2) + Name + " Registered!", ConsoleColor.Green, true);

            return true;
        }

        public bool Edit(string Name, string Command, bool IsProtected = false)
        {

            if (string.IsNullOrEmpty(Name.Trim()))
            {
                OnPrint?.Invoke("Alias register : Name is empty.".Push(2), ConsoleColor.Red, true, true);
                return false;
            }

            if (string.IsNullOrEmpty(Command.Trim()))
            {
                OnPrint?.Invoke("Alias register : Command is empty.".Push(2), ConsoleColor.Red, true, true);
                return false;
            }

            if (!Components.ContainsKey(Name))
            {
                OnPrint?.Invoke("Alias edit : Alias does not exist.".Push(2), ConsoleColor.Red, true, true);
                return false;
            }

            ((Alias)Components[Name]).Commands      = Command;
            ((Alias)Components[Name]).IsProtected   = IsProtected;

            Parent.ConfigurationSave();

            OnPrint?.Invoke("Alias ".Push(2) + Name + " Edited!", ConsoleColor.Green, true);

            return true;

        }

        public bool Remove(string Name) 
        {
            
            if (string.IsNullOrEmpty(Name.Trim()))
            {
                OnPrint?.Invoke("Alias remove : Name is empty.".Push(2), ConsoleColor.Red, true, true);
                return false;
            }

            if (!Components.ContainsKey(Name))
            {
                OnPrint?.Invoke("Alias remove : Name does not exist!".Push(2), ConsoleColor.Red, true, true);
                return false;
            }


            Components[Name]?.Dispose();

            Components[Name] = null;

            Components.Remove(Name);

            Parent.ConfigurationSave();

            OnPrint?.Invoke("Alias remove : ".Push(2) + Name + " removed!", ConsoleColor.Green, true);

            return true;
        }

        public void RemoveAll() 
        {
            string result = string.Empty;

            Components.Values.ForEach(c => {
                result += "Alias remove : " + c.CommandKey + " removed!\r\n";
                c.Dispose();
            });

            Components.Clear();

            Parent.ConfigurationSave();

            OnPrint?.Invoke(result, ConsoleColor.White, true, true);

        }

        #endregion

        #region "Interfaced Methods"

        public string List() 
        {

            string ret = string.Empty;

            Components.Values.ForEach(c => {
                ret += c.CommandKey.Push(2).FixedLength(20) + c.SyntaxHelp.FixedLength(40) + c.Description + "\r\n";
            });

            return ret + "\r\n";
        }

        public bool Run(string key, Output output, out object obj)
        {
            
            obj = null;

            if (string.IsNullOrEmpty(key.Trim()))
            {
                OnPrint?.Invoke("Alias Execute : Name is empty.".Push(2), ConsoleColor.Red, true, true);
                return false;
            }

            if (!Components.ContainsKey(key))
            {
                OnPrint?.Invoke("Alias Execute : Alias does not exist!".Push(2), ConsoleColor.Red, true, true);
                return false;
            }

            OnPrint?.Invoke("Alias Execute : Running ".Push(2) + key, ConsoleColor.White, true);

            Parent.DisplayCommands = false;
            Parent.Run(((Alias)Components[key])?.Commands);
            Parent.DisplayCommands = true;

            return true;

        }

        public void Parse(Output output)
        {

            List<string> subcommand = output.GetStack(Cmd);

            if (subcommand.Count > 1)
            {

                string  name        = subcommand.Where(o => o == "-n" || o == "--name").FirstOrDefault();
                string  value       = subcommand.Where(o => o == "-v" || o == "--value").FirstOrDefault();
                bool    isprotected = subcommand.Where(o => o == "-p" || o == "--protected").Any();

                switch (subcommand[1])
                {
                    case "help": // help
                        OnPrint?.Invoke(string.Empty);
                        OnPrint?.Invoke("help: command \"" + Cmd + "\"");
                        OnPrint?.Invoke(string.Empty);
                        OnPrint?.Invoke("-a or --add".Push(2).FixedLength(20) + "<OPTIONS>".FixedLength(30) + "This will add an alias.");
                        OnPrint?.Invoke("-e or --edit".Push(2).FixedLength(20) + "<OPTIONS>".FixedLength(30) + "This will edit an alias.");
                        OnPrint?.Invoke("-e or --edit".Push(2).FixedLength(20) + "<OPTIONS>".FixedLength(30) + "This will edit an alias.");
                        OnPrint?.Invoke("-r or --remove".Push(2).FixedLength(20) + "<OPTIONS>".FixedLength(30) + "This will remove an alias from the manager.");
                        OnPrint?.Invoke("-r or --remove".Push(2).FixedLength(20) + "-".FixedLength(30) + "This will remove all aliases from the manager.");
                        OnPrint?.Invoke("-l or --list".Push(2).FixedLength(20) + "-".FixedLength(30) + "This will list all aliases in the manager.");
                        OnPrint?.Invoke("The options are as follows".Push(2));
                        OnPrint?.Invoke("--".Push(2).FixedLength(10) + "-n or --name".FixedLength(30) + "n is for name as you would place the name after the option.");
                        OnPrint?.Invoke("--".Push(2).FixedLength(10) + "-v or --value".FixedLength(30) + "v is for name as you would place the name after the option.");
                        OnPrint?.Invoke("--".Push(2).FixedLength(10) + "-p or --protected".FixedLength(30) + "p makes the alias protected.");
                        OnPrint?.Invoke(string.Empty, ConsoleColor.Gray, true, true);
                        break;
                    case "--add":
                    case "-a":

                        if (!string.IsNullOrEmpty(name))
                            name = subcommand[(subcommand.IndexOf(name) + 1)].Replace("\"", string.Empty);
                        else
                        {
                            OnPrint?.Invoke("Expected: <name> <value>".Push(2), ConsoleColor.Red, true, true);
                            return;
                        }

                        if (name.StartsWith("-"))
                        {
                            OnPrint?.Invoke("Expected: <name>".Push(2), ConsoleColor.Red, true, true);
                            return;
                        }

                        if (!string.IsNullOrEmpty(value))
                            value = subcommand[(subcommand.IndexOf(value) + 1)].Replace("\"", string.Empty);
                        else
                        {
                            OnPrint?.Invoke("Expected: <name> <value>".Push(2), ConsoleColor.Red, true, true);
                            return;
                        }

                        if (value.StartsWith("-"))
                        {
                            OnPrint?.Invoke("Expected: <value>".Push(2), ConsoleColor.Red, true, true);
                            return;
                        }

                        Add(name, value, isprotected);

                        OnPrint?.Invoke(string.Empty, ConsoleColor.Gray, true, true);

                        break;
                    case "--edit":
                    case "-e":

                        if (!string.IsNullOrEmpty(name))
                            name = subcommand[(subcommand.IndexOf(name) + 1)].Replace("\"", string.Empty);
                        else
                        {
                            OnPrint?.Invoke("Expected: <name> <value>".Push(2), ConsoleColor.Red, true, true);
                            return;
                        }

                        if (name.StartsWith("-"))
                        {
                            OnPrint?.Invoke("Expected: <name>".Push(2), ConsoleColor.Red, true, true);
                            return;
                        }

                        if (!string.IsNullOrEmpty(value))
                            value = subcommand[(subcommand.IndexOf(value) + 1)].Replace("\"", string.Empty);
                        else
                        {
                            OnPrint?.Invoke("Expected: <name> <value>".Push(2), ConsoleColor.Red, true, true);
                            return;
                        }

                        if (value.StartsWith("-"))
                        {
                            OnPrint?.Invoke("Expected: <value>".Push(2), ConsoleColor.Red, true, true);
                            return;
                        }

                        Edit(name, value, isprotected);

                        OnPrint?.Invoke(string.Empty, ConsoleColor.Gray, true, true);

                        break;
                    case "--remove":
                    case "-r":
                        if (!string.IsNullOrEmpty(name))
                        {

                            name = subcommand[(subcommand.IndexOf(name) + 1)].Replace("\"", string.Empty);
                            
                            if (name.StartsWith("-"))
                            {
                                OnPrint?.Invoke("Expected: <name>".Push(2), ConsoleColor.Red, true, true);
                                return;
                            }

                            Parent.SetupQuestionAnswer(
                                "Are you sure you want to remove this alias?",
                                e => {
                                    if (e == Enums.OQuestionAnswer.Yes)
                                        Remove(name);
                                    else
                                        OnPrint?.Invoke("Operation cancelled.", ConsoleColor.Gray, true, true);
                                });
                        }
                        else
                        {
                            Parent.SetupQuestionAnswer(
                                "Are you sure you want to remove all aliases?", 
                                e => {
                                    if (e == Enums.OQuestionAnswer.Yes)
                                        RemoveAll();
                                    else
                                        OnPrint?.Invoke("Operation cancelled.", ConsoleColor.Gray, true, true);
                                });
                        }
                        break;
                    case "--list":
                    case "-l":
                        OnPrint?.Invoke(string.Empty);
                        OnPrint?.Invoke("Listed Aliases...", ConsoleColor.Yellow);
                        OnPrint?.Invoke("=".Strech(80));
                        OnPrint?.Invoke(List(), ConsoleColor.Green);
                        OnPrint?.Invoke(string.Empty, ConsoleColor.Gray, true, true);
                        break;

                    default:
                        OnPrint?.Invoke("Expected: <param>".Push(2), ConsoleColor.Red, true, true);
                        break;
                }
            }
            else
                OnPrint?.Invoke("Expected <param>".Push(2), ConsoleColor.Red, true, true);

        }

        public void LoadConfiguration(string json) 
        {
            var vars = JsonConvert
                .DeserializeObject<Dictionary<string, Alias>>(json);

            vars.ForEach(kp => {
                if (!Components.ContainsKey(kp.Key))
                    Components.Add(kp.Key, kp.Value);
            });

            Components.Keys.Where(x => !vars.Keys.Contains(x))
                .ForEach(key => {
                    Components[key].Dispose();
                    Components.Remove(key);
                });

        }
       
        public string SaveConfiguration()
        {
            return JsonConvert.SerializeObject(Components);
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
