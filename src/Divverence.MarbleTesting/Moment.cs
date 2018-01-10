namespace Divverence.MarbleTesting
{
    public class Moment
    {
        public Moment(int time, string[] marbles, bool isOrderedGroup = true)
        {
            Time = time;
            Marbles = marbles;
            IsOrderedGroup = isOrderedGroup;
        }

        public Moment(int time, string marble, bool isOrderedGroup = true) : this(time, new[] { marble }, isOrderedGroup)
        {
        }

        public Moment(int time) : this(time, new string[0])
        {
        }

        public int Time { get; }
        public string[] Marbles { get; }
        public bool IsOrderedGroup { get; }
    }
}