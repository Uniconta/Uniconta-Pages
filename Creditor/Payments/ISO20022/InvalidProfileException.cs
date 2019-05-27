using System;

namespace UnicontaISO20022CreditTransfer
{
    /// <summary>
    /// An exception thrown if a profile is used on an unsupported document. 
    /// </summary>
    public class InvalidProfileException : Exception
    {
        public InvalidProfileException(string message) : base(message)
        {

        }
    }
}

