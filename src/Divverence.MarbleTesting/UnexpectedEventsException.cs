using System;

namespace Divverence.MarbleTesting
{
    /// <summary>
    /// This exception should be used to signal the fact that unexpected events were received on a given moment.
    /// </summary>
    internal sealed class UnexpectedEventsException : Exception
    {
        public UnexpectedEventsException()
        {
        }

        public UnexpectedEventsException(string message) : base(message)
        {
        }

        public UnexpectedEventsException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}