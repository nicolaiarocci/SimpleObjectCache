using System;

namespace Amica.vNext.SimpleCache
{
    class ApplicationNameNullException : Exception
    {
        public override string Message => "Make sure to set ApplicationName on startup";
    }
    class TypeMismatchException : Exception
    {
    }
}
