/*
' /====================================================\
'| Developed Tony N. Hyde (www.k2host.co.uk)            |
'| Projected Started: 2020-03-16                        | 
'| Use: General                                         |
' \====================================================/
*/
using System;
using System.Diagnostics;
using System.IO;

using K2host.Console.Delegates;

namespace K2host.Console.Classes
{
    public class OConsoleCmdLauncher : IDisposable
    {

        public OutputEventHandler OutputReceived;
      
        public CloseEventHandler CloseEvent;

        StreamWriter StdIn { get; set; }

        Process CmdProcess { get; set; }

        public OConsoleCmdLauncher()
        {

            CmdProcess = new Process();
            CmdProcess.StartInfo.FileName = @"C:\Windows\System32\cmd.exe";
            CmdProcess.StartInfo.UseShellExecute = false;
            CmdProcess.StartInfo.RedirectStandardInput = true;
            CmdProcess.StartInfo.RedirectStandardOutput = true;
            CmdProcess.StartInfo.RedirectStandardError = true;
            CmdProcess.StartInfo.CreateNoWindow = true;
            CmdProcess.Start();

            StdIn = CmdProcess.StandardInput;

            CmdProcess.OutputDataReceived += Process_OutputDataReceived;
            CmdProcess.ErrorDataReceived += Process_OutputDataReceived;

            CmdProcess.BeginOutputReadLine();
            CmdProcess.BeginErrorReadLine();

        }

        public void SendCommand(string command)
        {

            if (command.Trim() == "exit")
                return;

            StdIn.WriteLine(command);

        }

        private void Process_OutputDataReceived(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (outLine.Data == null)
                return;
            else
                OutputReceived?.Invoke(this, new OConsoleCmdEventArgsForCommand() { OutputData = outLine.Data });

        }

        public void SyncClose()
        {
            StdIn.WriteLine("exit");
            CmdProcess.WaitForExit();
            CmdProcess.Close();
        }

        public void AsyncClose()
        {
            StdIn.WriteLine("exit");
            CmdProcess.Close();
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


            }

            IsDisposed = true;
        }

        #endregion

    }
}
