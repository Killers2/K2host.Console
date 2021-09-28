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
using System.Threading;
using System.IO;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.Emit;

using Newtonsoft.Json;

using K2host.Core;
using K2host.Console.Classes;
using K2host.Console.Delegates;
using K2host.Console.Interfaces;
using K2host.Console.Enums;
using K2host.Threading.Extentions;
using K2host.Threading.Classes;

namespace K2host.Console.Managers
{


    public class OManagerMacros : IConsoleCommand
    {

        #region "Embedded IConsoleComponent"

        public class Macro : IConsoleComponent
        {

            public string CommandKey { get; set; } = string.Empty;

            public string Description { get; set; } = string.Empty;

            public string SyntaxHelp { get; set; } = string.Empty;

            public string Code { get; set; } = string.Empty;
           
            public OConsoleMacroSyntax Syntax { get; set; } = OConsoleMacroSyntax.CSharp;

            public Macro() { }

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

        public string Cmd { get; set; } = "?";

        public string HelpSyntax { get; set; } = "<OPTION(S)>";

        public string Description { get; set; } = "This is the threaded macro service, for more help type sub command \"help\".";

        public Dictionary<string, IConsoleComponent> Components { get; private set; } = new();

        public string[] ReferencesScript { get; set; }

        public string[] ReferencesFiles { get; set; } = Array.Empty<string>();

        public OConsoleMacroSyntax EnvironmentSyntax { get; set; }

        #endregion

        #region "Constructor"

        public OManagerMacros() 
        {

            ReferencesScript = new string[] {
                "System",
                "System.IO",
                "System.Net",
                "System.Data",
                "System.Text",
                "System.Reflection",
                "System.Net.Sockets",
                "Microsoft.VisualBasic",
                "System.Runtime.InteropServices"
            };

        }

        #endregion

        #region "Methods"

        private string RefferencesVB()
        {

            string ret = string.Empty;

            foreach (string e in ReferencesScript)
                ret += "Imports " + e + "\r\n";

            return ret + "\r\n\r\n";

        }

        private string RefferencesCS()
        {

            string ret = string.Empty;

            foreach (string e in ReferencesScript)
                ret += "using " + e + ";" + "\r\n";

            return ret + "\r\n\r\n";
        }

        public Macro Add(Macro e)
        {
            
            if (Components.ContainsKey(e.CommandKey))
            {
                OnPrint?.Invoke("Macro:: This macro name is already in use.".Push(2), ConsoleColor.Red, true);
                return default;
            }

            Components.Add(e.CommandKey, e);

            Parent.ConfigurationSave();

            return e;

        }

        public bool Edit(string Name, string Field, string Value)
        {

            if (!Components.ContainsKey(Name))
            {
                OnPrint?.Invoke("Macro:: This macro does not exist.".Push(2), ConsoleColor.Red, true);
                return false;
            }

            Macro e = (Macro)Components[Name];

            if (Field.ToLower() == "name")
            {
                if (Components.ContainsKey(Value))
                {
                    OnPrint?.Invoke("Macro:: This name is already listed <".Push(2) + Value + ">, please try another.", ConsoleColor.Red, true);
                    return false;
                }
                Components.Remove(Name);
                Components.Add(Value, e);
            }

            switch (Field.ToLower()) {
                case "name":
                    e.CommandKey = Value;
                    break;
                case "code":
                    e.Code = Value;
                    break;
                case "help":
                    e.SyntaxHelp = Value;
                    break;
                case "description":
                    e.Description = Value;
                    break;
                case "syntax":
                    switch (Value.ToLower()) {
                        case "vb":
                            e.Syntax = OConsoleMacroSyntax.VisualBasic;
                            break;
                        case "cs":
                            e.Syntax = OConsoleMacroSyntax.CSharp;
                            break;
                        case "js":
                            e.Syntax = OConsoleMacroSyntax.JavaScript;
                            break;
                        case "vbs":
                            e.Syntax = OConsoleMacroSyntax.VisualBasicScript;
                            break;
                        default:
                            OnPrint?.Invoke("Macro:: This value is invalid <".Push(2) + Value + ">, expected <vb, cs, vbs, js>.", ConsoleColor.Red, true);
                            return false;
                    }
                    break;
                default:
                    OnPrint?.Invoke("Macro:: This field is invalid <".Push(2) + Field + ">, expected <name, code, help, description>.", ConsoleColor.Red, true);
                    return false;
            }

            Parent.ConfigurationSave();

            return true;

        }

        public bool Remove(string Name) 
        {
            
            if (string.IsNullOrEmpty(Name.Trim()))
            {
                OnPrint?.Invoke("Macro:: Name is empty.".Push(2), ConsoleColor.Red, true);
                return false;
            }

            if (!Components.ContainsKey(Name))
            {
                OnPrint?.Invoke("Macro:: This macro does not exist!".Push(2), ConsoleColor.Red, true);
                return false;
            }

            Components[Name]?.Dispose();

            Components[Name] = null;

            Components.Remove(Name);

            Parent.ConfigurationSave();

            OnPrint?.Invoke("Macro:: This macro ".Push(2) + Name + " has been removed.", ConsoleColor.Green, true);

            return true;
        }

        public void RemoveAll() 
        {

            Components.Values.ForEach(c => {
                OnPrint?.Invoke("Alias remove : ".Push(2) + c.CommandKey + " removed!", ConsoleColor.White, true);
                c.Dispose();
            });

            Parent.ConfigurationSave();

            Components.Clear();

        }
       
        private static string[] ParseCode(string code)
        {

            List<string> b = new();
            char[] c = _codebase.ToArray();
            string p = "$";

            if (!(code.IndexOf(p) > -1))
                return b.ToArray();

            string[] t = code.Split(p);

            for (int i = 0; i <= t.Length - 1; i++)
                if (i > 0)
                {
                    string r = t[i].Remove(t[i].IndexOfAny(c));
                    if (t[i].Length > 0)
                        b.Add(p + r.Trim());
                }

            return b.ToArray();

        }

        private void ExecuteMacro(object e) {

            if (e.GetType() == typeof(Macro))
            {
                object o = RunMacro((Macro)e);
                OnPrint?.Invoke(o?.ToString(), ConsoleColor.White, true, true);
            }

            if (e.GetType() == typeof(string))
            {

                Macro m = new()
                { 
                    CommandKey  = "m",
                    Syntax      = EnvironmentSyntax,
                    Code        = (string)e
                };

                try
                {
                    object o = RunMacro(m);
                    OnPrint?.Invoke(o?.ToString(), ConsoleColor.White, true, true);

                }
                catch
                {
                    OnPrint?.Invoke(string.Empty);
                }

                m.Dispose();

            }

        }

        public object RunMacro(Macro e) 
        {

            string code     = e.Code;
            string c        = string.Empty;
            object r        = null;

            switch (e.Syntax) {
                case OConsoleMacroSyntax.CSharp:
                    c = RefferencesCS() + _codebase_cs;
                    break;
                case OConsoleMacroSyntax.VisualBasic:
                    c = RefferencesVB() + _codebase_vb;
                    break;
            }

            string[] args = ParseCode(code);

            if (args.Length > 0)
            {

                foreach (string arg in args)
                {
                    if (!Components.ContainsKey(arg.Remove(0, 1).ToString()))
                    {
                        OnPrint?.Invoke("Macro Service: there is no macro named < ".Push(2) + arg.Remove(0, 1).ToString() + " >.", ConsoleColor.Red, true, true);
                        return null;
                    }

                    if (!Components.ContainsKey(arg.Remove(0, 1).ToString()))
                        return null;

                    Macro m = (Macro)Components[arg.Remove(0, 1).ToString()];

                    if (m.Code == string.Empty)
                    {
                        OnPrint?.Invoke("Macro Service: null (nothing) - no code.".Push(2), ConsoleColor.Red, true, true);
                        return null;
                    }

                    code = code.Replace(arg, (string)RunMacro(m).ToString().ToLower());

                }

            }

            string insertVars = string.Empty;
            var manager = (OManagerVariables)Parent.CommandManager.Values.FirstOrDefault(t => t.GetType() == typeof(OManagerVariables));
            if (manager != null)
            {
                manager.Components.ForEach(kp =>
                {
                    string varType = ((OManagerVariables.Variable)kp.Value).Value.GetType().Name;
                    string varName = kp.Key;
                    string varValue = ((OManagerVariables.Variable)kp.Value).Value.ToString();

                    if (varType == "string")
                        varValue = "" + varValue + "";

                    insertVars += varType + " " + varName + " = " + varValue + ";\r\n\r\n";

                });
            }

            c = c.Replace("<v>", insertVars);
            c = c.Replace("<?>", code);

            if(e.Syntax == OConsoleMacroSyntax.CSharp)
                Complier = CSharpSyntaxTree.ParseText(c);

            if (e.Syntax == OConsoleMacroSyntax.VisualBasic)
                Complier = VisualBasicSyntaxTree.ParseText(c);

            if (!CompileCode(e.Syntax))
            {
                ScriptErrors.ForEach(s => { OnPrint?.Invoke(s, ConsoleColor.Red, true, true); });
                return default;
            }

            var CodeInstanceMethod = CodeInstance.GetType().GetMethod("cxmacrofunc", BindingFlags.Public | BindingFlags.Instance);
            
            if (CodeInstanceMethod == null)
            {
                OnPrint?.Invoke("Macro service: Error reading method: cxmacro method is missing".Push(2), ConsoleColor.Red, true, true);
                return false;
            }

            try
            {
                r = CodeInstanceMethod.Invoke(CodeInstance, null);
                return r.ToString();
            }
            catch(Exception ex)
            {
                OnPrint?.Invoke("Macro service: There was a macro error: ".Push(2) + ex.Message, ConsoleColor.Red, true, true);
                return default;
            }

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
                OnPrint?.Invoke("Macro:: Name is empty.".Push(2), ConsoleColor.Red, true, true);
                return false;
            }

            if (!Components.ContainsKey(key))
            {
                OnPrint?.Invoke("Macro:: Macro does not exist!".Push(2), ConsoleColor.Red, true, true);
                return false;
            }

            Parent.ThreadManager
                .Add(new OThread(new ParameterizedThreadStart(ExecuteMacro)))
                .Start(Components[key]);

            return true;

        }

        public void Parse(Output output)
        {

            List<string> subcommand = output.GetStack(Cmd);
            
            string[] tmp = null;

            if (subcommand.Count > 1)
                switch (subcommand[1])
                {
                    case "syntax": //Syntax ? -s vb
                        switch (subcommand[2].ToLower())
                        {
                            case "vb":
                                EnvironmentSyntax = OConsoleMacroSyntax.VisualBasic;
                                break;
                            case "cs":
                                EnvironmentSyntax = OConsoleMacroSyntax.CSharp;
                                break;
                            default:
                                OnPrint?.Invoke("Expected:: <type> (vb, cs)".Push(2), ConsoleColor.Red, true, true);
                                return;
                        }
                        OnPrint?.Invoke("Macro:: syntax set to ".Push(2) + EnvironmentSyntax.ToString().ToUpper(), ConsoleColor.Red, true, true);
                        break;
                    case "add":
                        if (subcommand.Count == 7)
                        {
                            //? -a "NAME" "SYNTAX" "CODE" "HELP" "DESCRIPTION"

                            tmp = new string[] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };

                            tmp[1] = subcommand[2].Replace("\"", string.Empty);     //NAME
                            tmp[2] = subcommand[3].Replace("\"", string.Empty);     //TYPE
                            tmp[3] = subcommand[4].Replace("\"", string.Empty);     //CODE
                            tmp[4] = subcommand[5].Replace("\"", string.Empty);     //HELP
                            tmp[5] = subcommand[6].Replace("\"", string.Empty);     //DESCRIPTION

                            if (tmp[1] == string.Empty)
                            {
                                OnPrint?.Invoke("Macro:: no name given, please entre a name".Push(2), ConsoleColor.Red, true, true);
                                return;
                            }

                            if (tmp[1].IsNumeric())
                            {
                                OnPrint?.Invoke("Macro:: name cannot contain numbers, please entre a name".Push(2), ConsoleColor.Red, true, true);
                                return;
                            }

                            if (tmp[1].IndexOfAny("1234567890".ToCharArray()) > -1)
                            {
                                OnPrint?.Invoke("Macro:: name cannot contain numbers, please entre a name".Push(2), ConsoleColor.Red, true, true);
                                return;
                            }

                            if (tmp[2] == string.Empty)
                            {
                                OnPrint?.Invoke("Macro:: no type given, please entre a <type> (vb, cs)".Push(2), ConsoleColor.Red, true, true);
                                return;
                            }

                            if (tmp[3] == string.Empty)
                            {
                                OnPrint?.Invoke("Macro:: no code given, please type some code".Push(2), ConsoleColor.Red, true, true);
                                return;
                            }

                            if (tmp[4] == string.Empty)
                                tmp[4] = string.Empty;

                            if (tmp[5] == string.Empty)
                                tmp[5] = string.Empty;

                            if (Components.ContainsKey(tmp[1]))
                            {
                                OnPrint?.Invoke("Macro:: detected a macro with the same name, please try another name".Push(2), ConsoleColor.Red, true, true);
                                return;
                            }

                            OConsoleMacroSyntax t = OConsoleMacroSyntax.CSharp;

                            switch (tmp[2].ToString().ToLower())
                            {
                                case "vb":
                                    t = OConsoleMacroSyntax.VisualBasic;
                                    break;
                                case "cs":
                                    t = OConsoleMacroSyntax.CSharp;
                                    break;
                                default:
                                    OnPrint?.Invoke("Expected:: <type> (vb, cs)".Push(2), ConsoleColor.Red, true, true);
                                    return;
                            }

                            if (Add(new Macro() {
                                CommandKey  = tmp[1].ToString(),
                                Syntax      = t,
                                Code        = tmp[3].ToString(),
                                SyntaxHelp  = tmp[4].ToString(),
                                Description = tmp[5].ToString()
                            }) != null)
                            {
                                OnPrint?.Invoke("Macro:: added < ".Push(2) + tmp[1].ToString() + " >", ConsoleColor.Red, true, true);
                            }

                        }
                        else
                            OnPrint?.Invoke("Expected: <name> <syntax> <code> <help> <description>".Push(2), ConsoleColor.Red, true, true);
                        break;
                    case "edit":
                        if (subcommand.Count == 5)
                        {
                            //? -e "NAME" "FIELD" "VALUE"

                            tmp = new string[] { string.Empty, string.Empty, string.Empty, string.Empty };

                            tmp[1] = subcommand[2].Replace("\"", string.Empty);     //NAME
                            tmp[2] = subcommand[3].Replace("\"", string.Empty);     //FIELD
                            tmp[3] = subcommand[4].Replace("\"", string.Empty);     //VALUE

                            if (tmp[1] == string.Empty)
                            {
                                OnPrint?.Invoke("Macro:: no name given, please entre a name.".Push(2), ConsoleColor.Red, true, true);
                                return;
                            }

                            if (tmp[2] == string.Empty)
                            {
                                OnPrint?.Invoke("Macro:: no field given, please entre a field (name|code|help|description|syntax)".Push(2), ConsoleColor.Red, true, true);
                                return;
                            }

                            if (tmp[3] == string.Empty)
                            {
                                OnPrint?.Invoke("Macro:: no value given, please type some value".Push(2), ConsoleColor.Red, true, true);
                                return;
                            }

                            if (Edit(tmp[1].ToString(), tmp[2].ToString(), tmp[3].ToString()))
                                OnPrint?.Invoke("Macro:: updated macro < ".Push(2) + tmp[1].ToString() + " >", ConsoleColor.Red, true, true);

                        }
                        else
                            OnPrint?.Invoke("Expected: <name> <field> <value>".Push(2), ConsoleColor.Red, true, true);
                        break;
                    case "delete":
                        if (subcommand.Count == 3)
                        {

                            tmp = new string[] { string.Empty, string.Empty };

                            tmp[1] = subcommand[2].Replace("\"", string.Empty); //NAME

                            if (tmp[1] == string.Empty)
                            {
                                OnPrint?.Invoke("Macro:: no name given, please entre a name.".Push(2), ConsoleColor.Red, true, true);
                                return;
                            }

                            if (!Components.ContainsKey(tmp[1].ToString()))
                            {
                                OnPrint?.Invoke("Macro:: no detect macro with the name < ".Push(2) + tmp[1].ToString() + " >.", ConsoleColor.Red, true, true);
                                return;
                            }

                            if (Remove(tmp[1].ToString()))
                                OnPrint?.Invoke("Macro:: removed macro < ".Push(2) + tmp[1].ToString() + " >.", ConsoleColor.Red, true, true);

                        }
                        else
                            OnPrint?.Invoke("Expected: <name>".Push(2), ConsoleColor.Red, true);
                        break;
                    case "list":
                        OnPrint.Invoke(string.Empty);
                        OnPrint.Invoke("Listed Macros...", ConsoleColor.Yellow);
                        OnPrint.Invoke("=".Strech(80));
                        OnPrint.Invoke(List(), ConsoleColor.Green, true, true);
                        break;
                    case "view":
                        if (subcommand.Count == 3)
                        {
                            //? -v "NAME"

                            tmp = new string[] { string.Empty, string.Empty };

                            tmp[1] = subcommand[2].Replace("\"", string.Empty); //NAME

                            if (tmp[1] == string.Empty)
                            {
                                OnPrint?.Invoke("Macro:: no name given, please entre a name.".Push(2), ConsoleColor.Red, true, true);
                                return;
                            }

                            if (!Components.ContainsKey(tmp[1].ToString()))
                            {
                                OnPrint?.Invoke("Macro:: detected no macro with the name < ".Push(2) + tmp[1].ToString() + " >.", ConsoleColor.Red, true, true);
                                return;
                            }

                            Macro e = (Macro)Components[tmp[1].ToString()];

                            if (e == null)
                            {
                                OnPrint?.Invoke("Macro:: detected no macro with the name < ".Push(2) + tmp[1].ToString() + " >.", ConsoleColor.Red, true, true);
                                return;
                            }

                            OnPrint?.Invoke("Macro details < " + e.CommandKey + " >", ConsoleColor.Yellow);
                            OnPrint?.Invoke("--------------------------------------------------------------------", ConsoleColor.White);
                            OnPrint?.Invoke("Name:".Push(5).FixedLength(10) + e.CommandKey, ConsoleColor.Yellow);
                            OnPrint?.Invoke("Code:".Push(5).FixedLength(10) + e.Code, ConsoleColor.Yellow);
                            OnPrint?.Invoke("Syntax:".Push(5).FixedLength(10) + e.Syntax.ToString().ToUpper(), ConsoleColor.Yellow);
                            OnPrint?.Invoke("Help:".Push(5).FixedLength(10) + e.SyntaxHelp, ConsoleColor.Yellow);
                            OnPrint?.Invoke("Description:".Push(5).FixedLength(10) + e.Description, ConsoleColor.Yellow, true, true);
                        }
                        else
                            OnPrint?.Invoke("Expected: <name>".Push(2), ConsoleColor.Red, true, true);
                        break;
                    case "run":
                        if (subcommand.Count == 3)
                        {
                            //? -r "NAME"
                            tmp = new string[] { string.Empty, string.Empty };

                            tmp[1] = subcommand[2].Replace("\"", string.Empty); //NAME

                            if (tmp[1] == string.Empty)
                            {
                                OnPrint?.Invoke("Macro:: no name given, please entre a name.".Push(2), ConsoleColor.Red, true, true);
                                return;
                            }

                            if (!Components.ContainsKey(tmp[1].ToString()))
                            {
                                OnPrint?.Invoke("Macro:: detected no macro with the name < ".Push(2) + tmp[1].ToString() + " >.", ConsoleColor.Red, true, true);
                                return;
                            }

                            Macro m = (Macro)Components[tmp[1].ToString()];

                            if (m == null)
                            {
                                OnPrint?.Invoke("Macro:: detected no macro with the name < ".Push(2) + tmp[1].ToString() + " >.", ConsoleColor.Red, true, true);
                                return;
                            }

                            Parent.ThreadManager.Add(new OThread(new ParameterizedThreadStart(ExecuteMacro))).Start(m);

                        }
                        else
                            OnPrint?.Invoke("Expected: <name>".Push(2), ConsoleColor.Red, true, true);
                        break;
                    case "help": // HELP
                        OnPrint?.Invoke(string.Empty);
                        OnPrint?.Invoke("help: command \"" + Cmd + "\"");
                        OnPrint?.Invoke(string.Empty);
                        OnPrint?.Invoke("syntax".Push(2).FixedLength(10) + "<TYPE>".FixedLength(40) + "This will set the default syntax for running code from your textboxbase (vb|cs|vbs|js).");
                        OnPrint?.Invoke("add".Push(2).FixedLength(10) + "<NAME> <TYPE> <CODE> <HELP> <DESCRIPTION>".FixedLength(40) + "This will add a macro to the systems listings.");
                        OnPrint?.Invoke("edit".Push(2).FixedLength(10) + "<NAME> <FIELD> <VALUE>".FixedLength(40) + "This will edit a current macro in the system (name|code|help|description|syntax).");
                        OnPrint?.Invoke("delete".Push(2).FixedLength(10) + "-".FixedLength(40) + "This will remove a macro from the listings.");
                        OnPrint?.Invoke("view".Push(2).FixedLength(10) + "<NAME>".FixedLength(40) + "This print the details on a macro within the system list.");
                        OnPrint?.Invoke("run".Push(2).FixedLength(10) + "<NAME>".FixedLength(40) + "This will run a marco and pass back the value.");
                        OnPrint?.Invoke("list".Push(2).FixedLength(10) + "-".FixedLength(40) + "This will list all macros in the system.");
                        OnPrint?.Invoke("<CODE>".Push(2).FixedLength(10) + "-".FixedLength(40) + "This will run code directly as source and pass back the value.");
                        OnPrint?.Invoke("$<NAME>".Push(2).FixedLength(10) + "-".FixedLength(40) + "Same as ? -R <Name> Where $<Name> can be with in code to run a marco within code/macro.");
                        OnPrint?.Invoke(string.Empty, ConsoleColor.Gray, true, true);
                        break;
                    default:

                        //? "(1 > 0) ? true : false"
                        //? "($PI > 1) ? true : false"
                        //? "($PI > $TEP) ? true : false"
                        //? $PI

                        tmp = new string[] { string.Empty, string.Empty };

                        tmp[1] = subcommand[1].Replace("\"", string.Empty); //CODE

                        if (tmp[1].ToString().Remove(1) == "$")
                        {
                            Parent.DisplayCommands = false;
                            Parent.Run("? run " + tmp[1].ToString().Remove(0, 1));
                            Parent.DisplayCommands = true;
                            return;
                        }

                        if (tmp[1] == string.Empty)
                        {
                            OnPrint?.Invoke("Macro:: no code given, please entre some code.".Push(2), ConsoleColor.Red, true, true);
                            return;
                        }

                        Parent.ThreadManager.Add(new OThread(new ParameterizedThreadStart(ExecuteMacro))).Start(tmp[1]);

                        break;
                }
            else
                OnPrint?.Invoke("Expected <param>".Push(2), ConsoleColor.Red, true, true);

        }

        public void LoadConfiguration(string json) 
        {

            var vars = JsonConvert
                .DeserializeObject<Dictionary<string, Macro>>(json);

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

        #region "Codebase Strings"

        const string _codebase_cs   = "public class cxmacro\r\n{\r\n    public object cxmacrofunc()\r\n    {\r\n        <v>        return <?>;\r\n    }\r\n}";
        const string _codebase_vb   = "Public Class cxmacro\r\n    Public Function cxmacrofunc() As Object\r\n        <v>        Return <?>\r\n    End Function\r\nEnd Class";
        const string _codebase      = "1234567890!£%^&*()_+{}~@:<>?,/;'#[]-=\\|`¬/*";

        private readonly List<string>   ScriptErrors            = new();
        private SyntaxTree              Complier                = null;
        private Type                    ClassType               = null;
        private Assembly                CodeCompiled            = null;
        private object                  CodeInstance            = null;
       
        private bool CompileCode(OConsoleMacroSyntax syntax)
        {
            string className = "cxmacro";
          
            try
            {
                AppDomain.CurrentDomain.GetAssemblies().ForEach(a => {
                    try
                    {
                        if (Path.GetExtension(a.Location.ToString().ToLower()) == ".dll")
                            if (!ReferencesFiles.Contains(a.Location.ToString()))
                                ReferencesFiles = ReferencesFiles.Append(a.Location.ToString()).ToArray();
                    }
                    catch { }
                });
            }
            catch (Exception ex)
            {
                ScriptErrors.Add(ex.StackTrace);
                return default;
            }

            ScriptErrors.Clear();

            CodeCompiled = null;
            ClassType = null;
            CodeInstance = null;

            try
            {

                ReferencesFiles = ReferencesFiles.Concat(new[] {
                    typeof(System.Object).GetTypeInfo().Assembly.Location,
                    Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Runtime.dll")
                }).ToArray();


                Compilation compilation = null;

                if (syntax == OConsoleMacroSyntax.CSharp)
                {
                    compilation = CSharpCompilation.Create(
                        className,
                        new[] { Complier },
                        ReferencesFiles.Select(r => MetadataReference.CreateFromFile(r)).ToArray(),
                        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    );
                }

                if (syntax == OConsoleMacroSyntax.VisualBasic)
                {
                    compilation = VisualBasicCompilation.Create(
                        className,
                        new[] { Complier },
                        ReferencesFiles.Select(r => MetadataReference.CreateFromFile(r)).ToArray(),
                        new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    );
                }
 
                using (var ms = new MemoryStream())
                {

                    EmitResult result = compilation.Emit(ms);

                    if (!result.Success)
                    {
                        foreach (Diagnostic diagnostic in result.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error))
                            ScriptErrors.Add(string.Format("\t{0}: {1}", diagnostic.Id, diagnostic.GetMessage()));
                        return false;
                    }

                    ms.Seek(0, SeekOrigin.Begin);

                    CodeCompiled    = Assembly.Load(ms.ToArray());
                    ClassType       = CodeCompiled.GetType(className);
                    CodeInstance    = CodeCompiled.CreateInstance(ClassType.Name);

                }

                return true;

            }
            catch (Exception ex)
            {
                ScriptErrors.Add(ex.Message);
                return false;
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
