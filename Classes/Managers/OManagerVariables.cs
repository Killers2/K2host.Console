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


    public class OManagerVariables : IConsoleCommand
    {

        #region "Embedded IConsoleComponent"

        public class Variable : IConsoleComponent
        {

            public string CommandKey { get; set; } = string.Empty;

            public string Description { get; set; } = string.Empty;

            public string SyntaxHelp { get; set; } = string.Empty;

            public object Value { get; set; } = null;
           
            public bool IsProtected { get; set; } = false;

            public Variable() { }

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

        public string Cmd { get; set; } = "var";

        public string HelpSyntax { get; set; } = "<OPTION(S)>";

        public string Description { get; set; } = "This is the variable service, for more help type sub command \"help\".";

        public Dictionary<string, IConsoleComponent> Components { get; private set; } = new();

        #endregion

        #region "Constructor"

        public OManagerVariables() { }

        #endregion

        #region "Methods"

        public bool Add(string Name, object Val, string Description, bool IsProtected = false) 
        {
            if (string.IsNullOrEmpty(Name.Trim()))
            {
                OnPrint?.Invoke("Variable register : Name is empty.".Push(2), ConsoleColor.Red, true, true);
                return false;
            }

            if (Val == null)
            {
                OnPrint?.Invoke("Variable register : Value is empty.".Push(2), ConsoleColor.Red, true, true);
                return false;
            }

            Components.Add(Name, new Variable()
            {
                CommandKey  = Name,
                Value       = Val,
                Description = Description,
                IsProtected = IsProtected
            });

            Parent.ConfigurationSave();

            OnPrint?.Invoke("Variable ".Push(2) + Name + " Registered!", ConsoleColor.Green, true);

            return true;
        }

        public bool Edit(string Name, object Val, bool IsProtected = false)
        {

            if (string.IsNullOrEmpty(Name.Trim()))
            {
                OnPrint?.Invoke("Variable edit : Name is empty.".Push(2), ConsoleColor.Red, true, true);
                return false;
            }

            if (Val == null)
            {
                OnPrint?.Invoke("Variable edit : Value is empty.".Push(2), ConsoleColor.Red, true, true);
                return false;
            }

            if (!Components.ContainsKey(Name))
            {
                OnPrint?.Invoke("Variable edit : Variable does not exist.".Push(2), ConsoleColor.Red, true, true);
                return false;
            }

            ((Variable)Components[Name]).Value         = Val;
            ((Variable)Components[Name]).IsProtected   = IsProtected;
           
            Parent.ConfigurationSave();

            OnPrint?.Invoke("Variable ".Push(2) + Name + " Edited!", ConsoleColor.Green, true);

            return true;

        }

        public bool Remove(string Name) 
        {

            if (string.IsNullOrEmpty(Name.Trim()))
            {
                OnPrint?.Invoke("Variable remove : Name is empty.".Push(2), ConsoleColor.Red, true, true);
                return false;
            }

            if (!Components.ContainsKey(Name))
            {
                OnPrint?.Invoke("Variable remove : Variable does not exist!".Push(2), ConsoleColor.Red, true, true);
                return false;
            }


            Components[Name]?.Dispose();

            Components[Name] = null;

            Components.Remove(Name);

            Parent.ConfigurationSave();

            OnPrint?.Invoke("Variable remove : ".Push(2) + Name + " removed!", ConsoleColor.Green, true);

            return true;
        }

        public void RemoveAll() 
        {
            string result = string.Empty;

            Components.Values.ForEach(c => {
                result += "Variable remove : ".Push(2) + c.CommandKey + " removed!\r\n";
                c.Dispose();
            });

            Components.Clear();

            Parent.ConfigurationSave();

            OnPrint?.Invoke(result, ConsoleColor.Yellow, true, true);

        }
       
        public string GetType(string Name)
        {

            if (string.IsNullOrEmpty(Name.Trim()))
            {
                OnPrint?.Invoke("Variable get data type : Name is empty.".Push(2), ConsoleColor.Red, true, true);
                return string.Empty;
            }


            if (!Components.ContainsKey(Name))
            {
                OnPrint?.Invoke("Variable get data type : Variable does not exist.".Push(2), ConsoleColor.Red, true, true);
                return string.Empty;
            }

            return ((Variable)Components[Name]).Value.GetType().ToString();

        }

        #endregion

        #region "Interfaced Methods"

        public string List() 
        {

            string ret = string.Empty;

            Components.Values.ForEach(c => {
                ret += c.CommandKey.Push(2).FixedLength(20) + ((Variable)c).Value.GetType().ToString().FixedLength(20) + c.Description + "\r\n";
            });

            return ret + "\r\n";
        }

        public bool Run(string key, Output output, out object obj)
        {
            
            obj = null;

            if (string.IsNullOrEmpty(key.Trim()))
            {
                OnPrint?.Invoke("Variable get : Name is empty.".Push(2), ConsoleColor.Red, true, true);
                return false;
            }

            if (!Components.ContainsKey(key))
            {
                OnPrint?.Invoke("Variable get : Variable does not exist!".Push(2), ConsoleColor.Red, true, true);
                return false;
            }
            
            obj = ((Variable)Components[key]).Value;

            return true;

        }

        public void Parse(Output output)
        {

            List<string> subcommand = output.GetStack(Cmd);

            if (subcommand.Count > 1)
                switch (subcommand[1]) 
                {
                    case "help": // help
                        OnPrint?.Invoke(string.Empty);
                        OnPrint?.Invoke("help: command \"" + Cmd + "\"");
                        OnPrint?.Invoke(string.Empty);
                        OnPrint?.Invoke("-a or --add".Push(2).FixedLength(20) + "<NAME> <DATATYPE> <VALUE>".FixedLength(40) + "This will register a variable with a value.");
                        OnPrint?.Invoke("-a or --add".Push(2).FixedLength(20) + "<NAME> <DATATYPE> func <NAME> <PARMS>".FixedLength(40) + "This will register a variable with a value from a registerd function.");
                        OnPrint?.Invoke("-a or --add".Push(2).FixedLength(20) + "<NAME> <DATATYPE> var <NAME>".FixedLength(40) + "This will register a variable with a value from another variable.");
                        OnPrint?.Invoke("-a or --add".Push(2).FixedLength(20) + "<NAME> <DATATYPE> ? $<NAME>".FixedLength(40) + "This will register a variable with a value from a stored marco.");
                        OnPrint?.Invoke("-a or --add".Push(2).FixedLength(20) + "<NAME> <DATATYPE> ? <CODE>".FixedLength(40) + "This will register a variable with a value from some marco code.");
                        OnPrint?.Invoke("-e or --edit".Push(2).FixedLength(20) + "<NAME> <VALUE>".FixedLength(40) + "This will edit a value to the named variable.");
                        OnPrint?.Invoke("-e or --edit".Push(2).FixedLength(20) + "<NAME> func <NAME> <PARMS>".FixedLength(40) + "This will edit a value to the named variable from a registerd function.");
                        OnPrint?.Invoke("-e or --edit".Push(2).FixedLength(20) + "<NAME> var <NAME>".FixedLength(40) + "This will edit a value to the named variable from another variable.");
                        OnPrint?.Invoke("-e or --edit".Push(2).FixedLength(20) + "<NAME> ? $<NAME>".FixedLength(40) + "This will edit a value to the named variable from a stored marco.");
                        OnPrint?.Invoke("-e or --edit".Push(2).FixedLength(20) + "<NAME> ? <CODE>".FixedLength(40) + "This will edit a value to the named variable from some marco code.");
                        OnPrint?.Invoke("-r or --remove".Push(2).FixedLength(20) + "<NAME>".FixedLength(40) + "This will remove a registered variable.");
                        OnPrint?.Invoke("-r or --remove".Push(2).FixedLength(20) + "-".FixedLength(40) + "This will remove all registered variables.");
                        OnPrint?.Invoke("-g or --get".Push(2).FixedLength(20) + "<NAME>".FixedLength(40) + "This will return the value of the variable to the output.");
                        OnPrint?.Invoke("-l or --list".Push(2).FixedLength(20) + "-".FixedLength(40) + "This will list the registerd variables.");
                        OnPrint?.Invoke(string.Empty, ConsoleColor.White, true, true);
                        break;

                    case "--add":
                    case "-a":
                        if (subcommand.Count > 2)
                        {

                            object[] tmp = new object[] { null, null, null, null, null, null };

                            tmp[1] = subcommand[2].Replace("\"", string.Empty);        // Name
                            tmp[2] = subcommand[3].Replace("\"", string.Empty);        // Type
                            tmp[3] = subcommand[4].Replace("\"", string.Empty);        // Value (Optional) (func | var | ?)

                            if (subcommand.Count >= 6)
                            {
                                tmp[4] = subcommand[5].Replace("\"", string.Empty);    // function, var, (macro or code)
                                tmp[5] = subcommand[6].Replace("\"", string.Empty);    // function variable (value)
                            }

                            bool isProtected = false;

                            if (subcommand.Count >= 8)
                                isProtected = bool.Parse(subcommand[7]);

                            if (tmp[3].ToString() == "func")
                            {
                                OManagerCommands manager = (OManagerCommands)Parent
                                    .CommandManager
                                    .Values
                                    .FirstOrDefault(t => t.GetType() == typeof(OManagerCommands));
                                
                                if (manager.Components.ContainsKey(tmp[4].ToString()))
                                {
                                    manager.Run(
                                        tmp[4].ToString(),
                                        Parent.CommandParser.Parse(output.OriginalCommand.Remove(0, output.OriginalCommand.IndexOf(tmp[3].ToString()) + 5)),
                                        out object ret
                                    );
                                    tmp[3] = ret;
                                }
                                else
                                {
                                    OnPrint?.Invoke("Value could not be set!".Push(2), ConsoleColor.Red, true, true);
                                    return;
                                }
                            }

                            if (tmp[3].ToString() == "var")
                            {
                                if (Components.ContainsKey(tmp[4].ToString()))
                                {
                                    Run(tmp[4].ToString(), output, out object ret);
                                    tmp[3] = ret;
                                }
                                else
                                {
                                    OnPrint?.Invoke("Value could not be set!".Push(2), ConsoleColor.Red, true, true);
                                    return;
                                }
                            }

                            if (tmp[3].ToString() == "?")
                            {

                                OManagerMacros manager = (OManagerMacros)Parent
                                    .CommandManager
                                    .Values
                                    .FirstOrDefault(t => t.GetType() == typeof(OManagerMacros));

                                if (tmp[4].ToString().Remove(1) == "$")
                                {
                                    if (!manager.Components.ContainsKey(tmp[4].ToString().Remove(0, 1)))
                                    {
                                        OnPrint?.Invoke("Value could not be set!".Push(2), ConsoleColor.Red, true, true);
                                        return;
                                    }
                                    tmp[3] = manager.RunMacro((OManagerMacros.Macro)manager.Components[tmp[4].ToString().Remove(0, 1)]).ToString();
                                }
                                else
                                {
                                    tmp[4] = output.OriginalCommand.Remove(0, output.OriginalCommand.IndexOf(tmp[3].ToString()) + 2);
                                    OManagerMacros.Macro m = new() { 
                                        CommandKey  = "m",
                                        Syntax      = manager.EnvironmentSyntax,
                                        Code        = tmp[4].ToString()
                                    };
                                    tmp[3] = manager.RunMacro(m).ToString();
                                    m.Dispose();
                                    m = null;
                                }

                            }

                            switch (tmp[2].ToString().ToLower())
                            {
                                case "string":
                                case "char":
                                    if (tmp[3] == null)
                                        tmp[3] = "";

                                    Add(tmp[1].ToString(), tmp[3].ToString().Replace("\"", string.Empty), "User registered variable string", isProtected);
                                    OnPrint?.Invoke(string.Empty, ConsoleColor.White, true, true);
                                    break;
                                case "byte":
                                    if (tmp[3] == null)
                                        tmp[3] = "0";

                                    Add(tmp[1].ToString(), byte.Parse(tmp[3].ToString().Replace("\"", string.Empty)), "User registered variable byte", isProtected);
                                    OnPrint?.Invoke(string.Empty, ConsoleColor.White, true, true);
                                    break;
                                case "single":
                                case "float":
                                    if (tmp[3] == null)
                                        tmp[3] = "0.0";

                                    Add(tmp[1].ToString(), float.Parse(tmp[3].ToString().Replace("\"", string.Empty)), "User registered variable float", isProtected);
                                    OnPrint?.Invoke(string.Empty, ConsoleColor.White, true, true);
                                    break;
                                case "double":
                                    if (tmp[3] == null)
                                        tmp[3] = "0.0";

                                    Add(tmp[1].ToString(), double.Parse(tmp[3].ToString().Replace("\"", string.Empty)), "User registered variable float", isProtected);
                                    OnPrint?.Invoke(string.Empty, ConsoleColor.White, true, true);
                                    break;
                                case "short":
                                case "int16":
                                    if (tmp[3] == null)
                                        tmp[3] = "0";

                                    Add(tmp[1].ToString(), short.Parse(tmp[3].ToString().Replace("\"", string.Empty)), "User registered variable int16", isProtected);
                                    OnPrint?.Invoke(string.Empty, ConsoleColor.White, true, true);
                                    break;
                                case "integer":
                                case "int":
                                case "int32":
                                    if (tmp[3] == null)
                                        tmp[3] = "0";

                                    Add(tmp[1].ToString(), int.Parse(tmp[3].ToString().Replace("\"", string.Empty)), "User registered variable int", isProtected);
                                    OnPrint?.Invoke(string.Empty, ConsoleColor.White, true, true);
                                    break;
                                case "long":
                                case "int64":
                                    if (tmp[3] == null)
                                        tmp[3] = "0";

                                    Add(tmp[1].ToString(), long.Parse(tmp[3].ToString().Replace("\"", string.Empty)), "User registered variable long", isProtected);
                                    OnPrint?.Invoke(string.Empty, ConsoleColor.White, true, true);
                                    break;
                                case "bool":
                                case "boolean":
                                    if (tmp[3] == null)
                                        tmp[3] = "false";

                                    Add(tmp[1].ToString(), bool.Parse(tmp[3].ToString().Replace("\"", string.Empty)), "User registered variable bool", isProtected);
                                    OnPrint?.Invoke(string.Empty, ConsoleColor.White, true, true);
                                    break;
                                default:
                                    OnPrint?.Invoke("Invalid data type!".Push(2), ConsoleColor.Yellow, true, true);
                                    break;
                            }
                        }
                        else
                            OnPrint?.Invoke("Expected: <name> <type> <value>".Push(2), ConsoleColor.Red, true, true);
                        break;
                    case "--edit":
                    case "-e":
                        if (subcommand.Count > 2)
                        {

                            object[] tmp = new object[] { null, null, null, null, null, null };

                            tmp[1] = subcommand[2].Replace("\"", string.Empty);        // Name
                            tmp[2] = subcommand[3].Replace("\"", string.Empty);        // Value (Optional) (func | var | ?)
                            tmp[3] = subcommand[4].Replace("\"", string.Empty);        // function, var, mem (macro or code)
                            tmp[4] = subcommand[5].Replace("\"", string.Empty);        // function variable (value)

                            bool isProtected = false;

                            if (subcommand.Count >= 7)
                                isProtected = bool.Parse(subcommand[6]);

                            if (tmp[2].ToString() == "func")
                            {
                                OManagerCommands manager = (OManagerCommands)Parent
                                    .CommandManager
                                    .Values
                                    .FirstOrDefault(t => t.GetType() == typeof(OManagerCommands));

                                if (manager.Components.ContainsKey(tmp[3].ToString()))
                                {
                                    manager.Run(
                                        tmp[3].ToString(),
                                        Parent.CommandParser.Parse(output.OriginalCommand.Remove(0, output.OriginalCommand.IndexOf(tmp[2].ToString()) + 5)),
                                        out object ret
                                    );
                                    tmp[2] = ret;
                                }
                                else
                                {
                                    OnPrint?.Invoke("Value could not be set!".Push(2), ConsoleColor.Red, true, true);
                                    return;
                                }

                            }

                            if (tmp[2].ToString() == "var")
                            {
                                if (Components.ContainsKey(tmp[3].ToString()))
                                {
                                    Run(tmp[3].ToString(), output, out object ret);
                                    tmp[2] = ret;
                                }
                                else
                                {
                                    OnPrint?.Invoke("Value could not be set!".Push(2), ConsoleColor.Red, true, true);
                                    return;
                                }
                            }

                            if (tmp[2].ToString() == "?")
                            {

                                OManagerMacros manager = (OManagerMacros)Parent
                                    .CommandManager
                                    .Values
                                    .FirstOrDefault(t => t.GetType() == typeof(OManagerMacros));

                                if (tmp[3].ToString().Remove(1) == "$")
                                {
                                    if (!manager.Components.ContainsKey(tmp[3].ToString().Remove(0, 1)))
                                    {
                                        OnPrint?.Invoke("Value could not be set!".Push(2), ConsoleColor.Red, true, true);
                                        return;
                                    }
                                    tmp[2] = manager.RunMacro((OManagerMacros.Macro)manager.Components[tmp[3].ToString().Remove(0, 1)]).ToString();
                                }
                                else
                                {
                                    tmp[4] = output.OriginalCommand.Remove(0, output.OriginalCommand.IndexOf(tmp[2].ToString()) + 2);
                                    OManagerMacros.Macro m = new() { 
                                        CommandKey  = "m",
                                        Syntax      = manager.EnvironmentSyntax,
                                        Code        = tmp[4].ToString()
                                    };
                                    tmp[2] = manager.RunMacro(m).ToString();
                                    m.Dispose();
                                    m = null;
                                }
                            }

                            if (Components.ContainsKey(tmp[1].ToString()))
                            {

                                tmp[3] = GetType(tmp[1].ToString());

                                switch (tmp[3].ToString().ToLower())
                                {
                                    case "system.string":
                                        Edit(tmp[1].ToString(), tmp[2].ToString().Replace("\"", string.Empty), isProtected); 
                                        OnPrint?.Invoke(string.Empty, ConsoleColor.White, true, true);
                                        break;
                                    case "system.boolean":
                                        Edit(tmp[1].ToString(), bool.Parse(tmp[2].ToString().Replace("\"", string.Empty)), isProtected);
                                        OnPrint?.Invoke(string.Empty, ConsoleColor.White, true, true);
                                        break;
                                    case "system.byte":
                                        Edit(tmp[1].ToString(), byte.Parse(tmp[2].ToString().Replace("\"", string.Empty)), isProtected);
                                        OnPrint?.Invoke(string.Empty, ConsoleColor.White, true, true);
                                        break;
                                    case "system.single":
                                        Edit(tmp[1].ToString(), float.Parse(tmp[2].ToString().Replace("\"", string.Empty)), isProtected);
                                        OnPrint?.Invoke(string.Empty, ConsoleColor.White, true, true);
                                        break;
                                    case "system.double":
                                        Edit(tmp[1].ToString(), double.Parse(tmp[2].ToString().Replace("\"", string.Empty)), isProtected);
                                        OnPrint?.Invoke(string.Empty, ConsoleColor.White, true, true);
                                        break;
                                    case "system.int16":
                                        Edit(tmp[1].ToString(), short.Parse(tmp[2].ToString().Replace("\"", string.Empty)), isProtected);
                                        OnPrint?.Invoke(string.Empty, ConsoleColor.White, true, true);
                                        break;
                                    case "system.int32":
                                        Edit(tmp[1].ToString(), int.Parse(tmp[2].ToString().Replace("\"", string.Empty)), isProtected);
                                        OnPrint?.Invoke(string.Empty, ConsoleColor.White, true, true);
                                        break;
                                    case "system.int64":
                                        Edit(tmp[1].ToString(), long.Parse(tmp[2].ToString().Replace("\"", string.Empty)), isProtected);
                                        OnPrint?.Invoke(string.Empty, ConsoleColor.White, true, true);
                                        break;
                                    default:
                                        OnPrint?.Invoke("Warning: this console variable has an invalid datatype (".Push(2) + tmp[3].ToString() + ")!", ConsoleColor.Red, true, true);
                                        break;
                                }
                            }
                        }
                        else
                            OnPrint?.Invoke("Expected: <name> <value>".Push(2), ConsoleColor.Red, true, true);
                        break;
                    case "--remove":
                    case "-r":
                        if (subcommand.Count >= 2)
                        {
                            if (subcommand.Count == 3)
                            {
                                Remove(subcommand[2].Replace("\"", string.Empty));
                                OnPrint?.Invoke(string.Empty, ConsoleColor.White, true, true);
                            }
                            else
                            {
                                RemoveAll();
                                OnPrint?.Invoke(string.Empty, ConsoleColor.White, true, true);
                            }
                        }
                        else
                            OnPrint?.Invoke("Expected: <name>".Push(2), ConsoleColor.Red, true, true);
                        break;
                    case "--get":
                    case "-g":
                        if (subcommand.Count > 2)
                        {
                            Run(subcommand[2].Replace("\"", string.Empty), output, out object ret);
                            OnPrint?.Invoke(subcommand[2].Push(2) + " (" + GetType(subcommand[2]) + ") = \"" + ret.ToString() + "\"");
                            OnPrint?.Invoke(string.Empty, ConsoleColor.White, true, true);
                        }
                        else
                            OnPrint?.Invoke("Expected <name>".Push(2), ConsoleColor.Red, true, true);
                        break;
                    case "--list":
                    case "-l":
                        OnPrint?.Invoke(string.Empty);
                        OnPrint?.Invoke("Listed Variables...", ConsoleColor.Yellow);
                        OnPrint?.Invoke("=".Strech(80));
                        OnPrint?.Invoke(List(), ConsoleColor.Green);
                        OnPrint?.Invoke(string.Empty, ConsoleColor.White, true, true);
                        break;

                    default:
                        OnPrint?.Invoke("Expected: <param>".Push(2), ConsoleColor.Red, true, true);
                        break;
                }
            else
                OnPrint?.Invoke("Expected <param>".Push(2), ConsoleColor.Red, true, true);

        }

        public void LoadConfiguration(string json) 
        {

            var vars = JsonConvert
                .DeserializeObject<Dictionary<string, Variable>>(json);

            vars.ForEach(kp => {
                    if(!Components.ContainsKey(kp.Key))
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
