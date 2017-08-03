using System;
using System.Threading.Tasks;

namespace Divverence.MarbleTesting
{
    public class InputMarble
    {
        public InputMarble(int time, string marble, Func<Task> action)
        {
            Time = time;
            Marble = marble;
            Action = action;
        }

        public int Time { get; }
        public string Marble { get; }
        public Func<Task> Action { get; }
    }
}