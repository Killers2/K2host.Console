/*
' /====================================================\
'| Redesigned Tony N. Hyde (www.k2host.co.uk)           |
'| Projected Started: 2020-03-16                        | 
'| Use: General                                         |
' \====================================================/
*/

namespace K2host.Console.Classes.CommandParsing
{
    public class OConsoleEditorAnsi
    {
        public static void ClearScreen()
        {
            System.Console.Clear();
        }

        public static void MoveCursor(int row, int col)
        {
            System.Console.CursorTop    = row;
            System.Console.CursorLeft   = col;
        }
    }
}
