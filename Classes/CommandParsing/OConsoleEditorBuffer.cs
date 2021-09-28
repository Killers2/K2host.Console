/*
' /====================================================\
'| Redesigned Tony N. Hyde (www.k2host.co.uk)           |
'| Projected Started: 2020-03-16                        | 
'| Use: General                                         |
' \====================================================/
*/

using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace K2host.Console.Classes.CommandParsing
{
    public class OConsoleEditorBuffer
    {
        readonly string[] _lines;

        public OConsoleEditorBuffer(IEnumerable<string> lines)
        {
            _lines = lines.ToArray();
        }

        public void Render()
        {
            byte[] b = Encoding.ASCII.GetBytes(string.Join("\r\n", _lines));
            using var stdout = System.Console.OpenStandardOutput();
            stdout.Write(b, 0, b.Length);
        }

        public int LineCount()
        {
            return _lines.Length;
        }

        public int LineLength(int row)
        {
            return _lines[row].Length;
        }

        public string GetBuffer()
        {
            return string.Join("\r\n", _lines);
        }

        internal OConsoleEditorBuffer Insert(string character, int row, int col)
        {
            var linesDeepCopy = _lines.Select(x => x).ToArray();
            linesDeepCopy[row] = linesDeepCopy[row].Insert(col, character);
            return new OConsoleEditorBuffer(linesDeepCopy);
        }

        internal OConsoleEditorBuffer Delete(int row, int col)
        {
            var linesDeepCopy = _lines.Select(x => x).ToArray();
            linesDeepCopy[row] = linesDeepCopy[row].Remove(col, 1);
            return new OConsoleEditorBuffer(linesDeepCopy);
        }
       
        internal OConsoleEditorBuffer DeleteLine(int row)
        {
            var linesDeepCopy = _lines.Select(x => x).ToArray();
            linesDeepCopy[row] = string.Empty;
            return new OConsoleEditorBuffer(linesDeepCopy);
        }

        internal OConsoleEditorBuffer SplitLine(int row, int col)
        {
            var linesDeepCopy = _lines.Select(x => x).ToList();

            var line = linesDeepCopy[row];

            var newLines = new[] { 
                line.Substring(0, col), 
                line.Substring(col, line.Length - line.Substring(0, col).Length) 
            };

            linesDeepCopy[row] = newLines[0];
            linesDeepCopy.Insert(row + 1, newLines[1]);

            return new OConsoleEditorBuffer(linesDeepCopy);
        }
       
        internal static OConsoleEditorBuffer ClearBuffer()
        {
            return new OConsoleEditorBuffer(new string[] { "" });
        }

    }
}
