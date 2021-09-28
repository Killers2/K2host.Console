/*
' /====================================================\
'| Developed Tony N. Hyde (www.k2host.co.uk)            |
'| Projected Started: 2020-03-16                        | 
'| Use: General                                         |
' \====================================================/
*/
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using K2host.Core;

namespace K2host.Console
{

    public static class OHelpers
    {

        public static string FormatListMethodsParameters(this ParameterInfo[] e)
        {

            string r = string.Empty;

            e.ForEach(p => {
                r += "<" + p.Name + "(" + p.ParameterType.ToString() + ")> ";
            });

            if (e.Length > 0)
                r = r.Remove(r.Length - 1);

            return r;

        }

        public static string Fsrm(this FileSystemRights e)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) 
            { 
                return e switch
                {
                    FileSystemRights.FullControl => "fc",
                    FileSystemRights.ListDirectory => "ld",
                    FileSystemRights.CreateFiles => "cf",
                    FileSystemRights.AppendData => "ad",
                    FileSystemRights.ReadExtendedAttributes => "rea",
                    FileSystemRights.WriteExtendedAttributes => "wea",
                    FileSystemRights.ExecuteFile => "x",
                    FileSystemRights.DeleteSubdirectoriesAndFiles => "dsdf",
                    FileSystemRights.ReadAttributes => "ra",
                    FileSystemRights.WriteAttributes => "wa",
                    FileSystemRights.Write => "w",
                    FileSystemRights.Delete => "d",
                    FileSystemRights.ReadPermissions => "rp",
                    FileSystemRights.Read => "r",
                    FileSystemRights.ReadAndExecute => "rx",
                    FileSystemRights.Modify => "m",
                    FileSystemRights.ChangePermissions => "cp",
                    FileSystemRights.TakeOwnership => "to",
                    FileSystemRights.Synchronize => "s",
                    _ => string.Empty
                };
            }
            return string.Empty;
        }
    }

}
