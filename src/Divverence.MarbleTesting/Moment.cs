namespace Divverence.MarbleTesting
{
    public class Moment
    {
        public Moment(int time, string[] marbles)
        {
            Time = time;
            Marbles = marbles;
        }

        public Moment(int time, string marble) : this(time, new[] {marble})
        {
        }

        public Moment(int time) : this(time, new string[0])
        {
        }

        public int Time { get; }
        public string[] Marbles { get; }
    }
}