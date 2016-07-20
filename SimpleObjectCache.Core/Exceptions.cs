using System;

namespace SimpleObjectCache
{
    class SimpleObjectCacheApplicationNameNullException : Exception
    {
        public override string Message => "Make sure to set ApplicationName on startup";
    }
    class SimpleObjectCacheTypeMismatchException : Exception
    {
    }
}
