using System;
using System.Runtime.Serialization;

namespace NonSteamShortcuts.Exceptions
{
    class NonSteamShortcutsException : Exception
    {
        public NonSteamShortcutsException()
        {
        }

        public NonSteamShortcutsException(string message) : base(message)
        {
        }

        public NonSteamShortcutsException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NonSteamShortcutsException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
