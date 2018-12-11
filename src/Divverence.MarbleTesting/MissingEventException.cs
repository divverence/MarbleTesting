using System;

namespace Divverence.MarbleTesting
{
    /// <summary>
    /// This exception should be used to signal the fact that not all expected events where received on a given moment.
    /// </summary>
    internal sealed class MissingEventException : Exception
    {
        public MissingEventException()
        {
        }

        public MissingEventException(string message) : base(message)
        {
        }

        public MissingEventException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}