using System;

namespace Divverence.MarbleTesting
{
    internal class ExpectedMarble
    {
        internal ExpectedMarble(int time, string marble, Action assertion)
        {
            Time = time;
            Marble = marble;
            Assertion = assertion;
        }

        public int Time { get; }
        public string Marble { get; }
        public Action Assertion { get; }
    }
}