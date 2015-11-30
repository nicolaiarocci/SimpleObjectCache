using System;

namespace Amica.vNext
{
    class SimpleCacheApplicationNameNullException : Exception
    {
        public override string Message => "Make sure to set ApplicationName on startup";
    }
    class SimpleCacheTypeMismatchException : Exception
    {
    }
}
