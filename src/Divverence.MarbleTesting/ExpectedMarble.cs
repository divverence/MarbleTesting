using System;

namespace Divverence.MarbleTesting
{
    public class ExpectedMarble
    {
        public ExpectedMarble(int time, string marble, Action assertion)
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