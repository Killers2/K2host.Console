/*
' /====================================================\
'| Redesigned Tony N. Hyde (www.k2host.co.uk)           |
'| Projected Started: 2020-03-16                        | 
'| Use: General                                         |
' \====================================================/
*/

using System;

namespace K2host.Console.Classes.CommandParsing
{
    public class OConsoleEditorCursor
    {
        
        public int Row { get; private set; }
       
        public int Col { get; private set; }

        public OConsoleEditorCursor(int row = 0, int col = 0)
        {
            Row = row;
            Col = col;
        }

        internal OConsoleEditorCursor Up(OConsoleEditorBuffer buffer)
        {
            return new OConsoleEditorCursor(Row - 1, Col).Clamp(buffer);
        }

        internal OConsoleEditorCursor Down(OConsoleEditorBuffer buffer)
        {
            return new OConsoleEditorCursor(Row + 1, Col).Clamp(buffer);
        }


        internal OConsoleEditorCursor Left(OConsoleEditorBuffer buffer)
        {
            return new OConsoleEditorCursor(Row, Col - 1).Clamp(buffer);
        }

        internal OConsoleEditorCursor Right(OConsoleEditorBuffer buffer)
        {
            return new OConsoleEditorCursor(Row, Col + 1).Clamp(buffer);
        }

        private OConsoleEditorCursor Clamp(OConsoleEditorBuffer buffer)
        {
            Row = Math.Min(buffer.LineCount() - 1, Math.Max(Row, 0));
            Col = Math.Min(buffer.LineLength(Row), Math.Max(Col, 0));
            return new OConsoleEditorCursor(Row, Col);
        }

        internal OConsoleEditorCursor MoveToCol(int col)
        {
            return new OConsoleEditorCursor(Row, col);
        }
    }
}
