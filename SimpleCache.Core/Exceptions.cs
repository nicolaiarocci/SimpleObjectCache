using System;

namespace SimpleCache
{
    class SimpleCacheApplicationNameNullException : Exception
    {
        public override string Message => "Make sure to set ApplicationName on startup";
    }
    class SimpleCacheTypeMismatchException : Exception
    {
    }
}
