using System;

namespace RouterControl.Models
{
    internal class LogEntryModel
    {
        public string Text { get; }
        public string Time { get; }

        public LogEntryModel(string text, DateTime time)
        {
            Text = text;
            Time = $"{time:HH:mm:ss:ffff}";
        }
    }
}