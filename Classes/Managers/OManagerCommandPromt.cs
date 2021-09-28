/*
' /====================================================\
'| Developed Tony N. Hyde (www.k2host.co.uk)            |
'| Projected Started: 2020-03-16                        | 
'| Use: General                                         |
' \====================================================/
*/

using System;
using System.Collections.Generic;

using K2host.Core;
using K2host.Console.Classes;
using K2host.Console.Delegates;
using K2host.Console.Interfaces;

namespace K2host.Console.Managers
{

    public class OManagerCommandPromt : IConsoleCommand
    {

        #region "Properties"

        public IConsole Parent { get; set; }

        public OnPrintEventHandler OnPrint { get; set; }

        public string Cmd { get; set; } = "cmd";

        public string HelpSyntax { get; set; } = "<OPTION(S)>";

        public string Description { get; set; } = "This command promt redirector allows access to the windows command promt here.";

        public Dictionary<string, IConsoleComponent> Components { get; private set; } = new();

        public OConsoleCmdLauncher CmdInstance { get; private set; }

        #endregion

        #region "Constructor"

        public OManagerCommandPromt() { }

        #endregion

        #region "Interfaced Methods"

        public string List()
        {
            return string.Empty;
        }

        public bool Run(string key, Output output, out object obj)
        {
            
            obj = null;

            if (string.IsNullOrEmpty(key.Trim()))
            {
                OnPrint?.Invoke("CMD Redirect : no key.".Push(2), ConsoleColor.Red, true, true);
                return false;
            }

            if (key.Trim() != "redirect")
            {
                OnPrint?.Invoke("CMD Redirect : no key.".Push(2), ConsoleColor.Red, true, true);
                return false;
            }

            CmdInstance.SendCommand(output.OriginalCommand);

            return true;

        }

        public void Parse(Output output)
        {

            List<string> subcommand = output.GetStack(Cmd);

            if (subcommand.Count > 1)
                switch (subcommand[1]) 
                {
                    case "on":
                        if (Parent.RedirectCommand != null)
                            OnPrint?.Invoke("ConsoleX::Exception: There is already a redirect set from: " + Parent.RedirectCommand.GetType().Name, ConsoleColor.Red, true, true);
                        else {
                            CmdInstance = new OConsoleCmdLauncher() {
                                OutputReceived = (object sendingProcess, OConsoleCmdEventArgsForCommand e) => {
                                    OnPrint?.Invoke(e.OutputData); 
                                }
                            };
                            Parent.RedirectCommand = this;
                        }
                        break;
                    case "off":
                        if (CmdInstance != null)
                        {
                            CmdInstance.AsyncClose();
                            CmdInstance.Dispose();
                            CmdInstance = null;
                            Parent.RedirectCommand = null;
                            OnPrint?.Invoke(string.Empty, ConsoleColor.White, true, true);
                        }
                        break;
                    case "help":
                        OnPrint?.Invoke(string.Empty);
                        OnPrint?.Invoke("help: command \"" + Cmd + "\"");
                        OnPrint?.Invoke(string.Empty);
                        OnPrint?.Invoke("on".Push(2).FixedLength(10) + "-".FixedLength(10) + "Starts the windows command promt in this console and redirects the input and output.");
                        OnPrint?.Invoke("off".Push(2).FixedLength(10) + "-".FixedLength(10) + "Returns the console back to normal and closes the running windows command promt process.");
                        OnPrint?.Invoke(string.Empty, ConsoleColor.White, true, true);
                        break;
                    default:
                        OnPrint?.Invoke("Expected: <param>".Push(2), ConsoleColor.Red, true, true);
                        break;
                }
            else
                OnPrint?.Invoke("Expected <param>".Push(2), ConsoleColor.Red, true, true);

        }

        public void LoadConfiguration(string json) { }

        public string SaveConfiguration() => string.Empty;

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
