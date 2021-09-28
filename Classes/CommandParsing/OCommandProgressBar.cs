/*
' /====================================================\
'| Developed marioinfang/ProgressBar.cs                 |
'| Forked from co89757/ProgressBar.cs                   | 
'| Use: General (A few mods)                            |
' \====================================================/
*/
using System;
using System.Text;
using System.Threading;

namespace K2host.Console.Classes
{
	/// <summary>
	/// co89757/ProgressBar.cs CMD ProgressBar
	/// </summary>
	public class OCommandProgressBar : IDisposable, IProgress<double>
    {

		//Useage
        //using (var p = new OCommandProgressBar())
        //for (int i = 0; i <= 100; i++)
        //{
        //    p.Report((double)i / 100);
        //    Thread.Sleep(5);
        //}

		private readonly TimeSpan animationInterval = TimeSpan.FromSeconds(1.0 / 8);
		private const string animation = @"|/-\";

		private readonly Timer timer;

		private double	currentProgress = 0;
		private string	currentText = string.Empty;

		private int		animationIndex = 0;

		public int BarWidth { get; set; }	= 40;

		public char CharDone { get; set; }	= '=';

		public char CharEmpty { get; set; } = ' ';

		public OCommandProgressBar()
		{
			timer = new Timer(TimerHandler, new object(), animationInterval, animationInterval);
		}

		public void Report(double value)
		{
			// Make sure value is in [0..1] range
			value = Math.Max(0, Math.Min(1, value));
			Interlocked.Exchange(ref currentProgress, value);
		}

		private void TimerHandler(object state)
		{
			lock (timer)
			{
				if (IsDisposed) return;

				int progressBlockCount = (int)(currentProgress * BarWidth);
				int percent = (int)(currentProgress * 100);
				string text = string.Format("[{0}{1}] {2,3}% {3}",
					new string(CharDone, progressBlockCount), new string(CharEmpty, BarWidth - progressBlockCount),
					percent,
					animation[animationIndex++ % animation.Length]);
				UpdateText(text);
			}
		}

		private void UpdateText(string text)
		{
			// Get length of common portion
			int commonPrefixLength = 0;
			int commonLength = Math.Min(currentText.Length, text.Length);
			while (commonPrefixLength < commonLength && text[commonPrefixLength] == currentText[commonPrefixLength])
			{
				commonPrefixLength++;
			}

			// Backtrack to the first differing character
			StringBuilder outputBuilder = new();
			outputBuilder.Append('\b', currentText.Length - commonPrefixLength);

			// Output new suffix
			outputBuilder.Append(text[commonPrefixLength..]);

			// If the new text is shorter than the old one: delete overlapping characters
			int overlapCount = currentText.Length - text.Length;
			if (overlapCount > 0)
			{
				outputBuilder.Append(' ', overlapCount);
				outputBuilder.Append('\b', overlapCount);
			}

			System.Console.Write(outputBuilder);
			currentText = text;
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
				lock (timer)
				{
					UpdateText(string.Empty);
					timer.Dispose();
				}
			}

			IsDisposed = true;
		}

		#endregion

	}
}
