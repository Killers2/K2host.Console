/*
' /====================================================\
'| Redesigned Tony N. Hyde (www.k2host.co.uk)           |
'| Projected Started: 2020-03-16                        | 
'| Use: General                                         |
' \====================================================/
*/
using System;
//using System.Windows;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using K2host.Console.Delegates;

namespace K2host.Console.Classes.CommandParsing
{
    public class OConsoleEditor
    {

        public CloseEventHandler OnClose { get; set; }

        public SaveEventHandler OnSave { get; set; }

        OConsoleEditorBuffer _buffer;
        OConsoleEditorCursor _cursor;
        Stack<object> _history;

        public string IoFile { get; set; } = string.Empty;

        public object SaveType { get; set; }

        public OConsoleEditor(int maxlines)
        {
            _buffer     = new OConsoleEditorBuffer(Enumerable.Repeat(string.Empty, maxlines).ToArray());
            _cursor     = new OConsoleEditorCursor();
            _history    = new Stack<object>();
        }

        public OConsoleEditor(string file)
        {
            IoFile      = file;
            _buffer     = new OConsoleEditorBuffer(File.ReadAllLines(IoFile).Where(x => x != Environment.NewLine));
            _cursor     = new OConsoleEditorCursor();
            _history    = new Stack<object>();
        }

        public void LoadFile(string file) 
        {
            IoFile      = file;
            _buffer     = new OConsoleEditorBuffer(File.ReadAllLines(IoFile).Where(x => x != Environment.NewLine));
            _history    = new Stack<object>();
        }
        
        public void LoadLines(string[] lines)
        {
            _buffer = new OConsoleEditorBuffer(lines);
            _history = new Stack<object>();
        }
      
        public void LoadContent(string content)
        {
            _buffer = new OConsoleEditorBuffer(content.Split("\r\n"));
            _history = new Stack<object>();
        }

        public void ClearBuffer() 
        {
            _buffer = OConsoleEditorBuffer.ClearBuffer();
        }

        public void Render()
        {
            OConsoleEditorAnsi.ClearScreen();
            OConsoleEditorAnsi.MoveCursor(0, 0);
            _buffer.Render();
            OConsoleEditorAnsi.MoveCursor(_cursor.Row, _cursor.Col);
        }

        public void HandleInput()
        {
           
            var character = System.Console.ReadKey();

            if (character.Key == ConsoleKey.UpArrow)
                _cursor = _cursor.Up(_buffer);

            else if (character.Key == ConsoleKey.DownArrow)
                _cursor = _cursor.Down(_buffer);

            else if (character.Key == ConsoleKey.LeftArrow)
                _cursor = _cursor.Left(_buffer);

            else if (character.Key == ConsoleKey.RightArrow)
                _cursor = _cursor.Right(_buffer);

            else if (character.Key == ConsoleKey.Tab)
            {
                SaveSnapshot();
                _buffer = _buffer.Insert("    ", _cursor.Row, _cursor.Col);
                //_cursor = _cursor.MoveToCol(_buffer.LineLength(_cursor.Row));
            }

            else if ((ConsoleModifiers.Control & character.Modifiers) == ConsoleModifiers.Control && character.Key == ConsoleKey.Q)
                OnClose?.Invoke();

            else if ((ConsoleModifiers.Control & character.Modifiers) == ConsoleModifiers.Control && character.Key == ConsoleKey.U)
                RestoreSnapshot();

            else if ((ConsoleModifiers.Control & character.Modifiers) == ConsoleModifiers.Control && character.Key == ConsoleKey.S)
                OnSave?.Invoke(_buffer);

            //else if ((ConsoleModifiers.Control & character.Modifiers) == ConsoleModifiers.Control && character.Key == ConsoleKey.V)
            //{
               // SaveSnapshot();
                //_buffer = _buffer.Insert(Clipboard.GetText(TextDataFormat.Text), _cursor.Row, _cursor.Col);
                //_cursor = new OConsoleEditorCursor();
            //}

            else if ((ConsoleModifiers.Control & character.Modifiers) == ConsoleModifiers.Control && character.Key == ConsoleKey.K)
            {
                SaveSnapshot();
                _buffer = _buffer.DeleteLine(_cursor.Row);
                _cursor = _cursor.MoveToCol(0);
            }

            else if ((ConsoleModifiers.Control & character.Modifiers) == ConsoleModifiers.Control && character.Key == ConsoleKey.X)
            {
                SaveSnapshot();
                _buffer = OConsoleEditorBuffer.ClearBuffer();
                _cursor = new OConsoleEditorCursor();
            }

            else if (character.Key == ConsoleKey.Backspace)
            {

                if (_cursor.Col == 0 && _cursor.Row == 0)
                {
                    //Do Nothing for now, but this is the start of the buffer
                }
                else if (_cursor.Col > 0)
                {
                    SaveSnapshot();
                    _buffer = _buffer.Delete(_cursor.Row, _cursor.Col - 1);
                    _cursor = _cursor.Left(_buffer);
                }
                else
                {
                    SaveSnapshot();
                    _cursor = _cursor.Up(_buffer);
                    _cursor = _cursor.MoveToCol(_buffer.LineLength(_cursor.Row));
                    if (_cursor.Col > 0)
                        _buffer = _buffer.Delete(_cursor.Row, _cursor.Col - 1);
                    _cursor = _cursor.Left(_buffer);
                }

            }

            else if (character.Key == ConsoleKey.Enter)
            {
                SaveSnapshot();
                _buffer = _buffer.SplitLine(_cursor.Row, _cursor.Col);
                _cursor = _cursor.Down(_buffer).MoveToCol(0);
            }

            else if (!char.IsControl(character.KeyChar))
            {
                SaveSnapshot();
                _buffer = _buffer.Insert(character.KeyChar.ToString(), _cursor.Row, _cursor.Col);
                _cursor = _cursor.Right(_buffer);
            }


        }

        private void SaveSnapshot()
        {
            _history.Push(_cursor);
            _history.Push(_buffer);
        }

        private void RestoreSnapshot()
        {
            if (_history.Count > 0)
            {
                _buffer = (OConsoleEditorBuffer)_history.Pop();
                _cursor = (OConsoleEditorCursor)_history.Pop();
            }
        }

    }
}
