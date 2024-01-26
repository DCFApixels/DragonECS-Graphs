using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace DCFApixels.DragonECS.Relations.Internal
{
    internal static class Throw
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ArgumentNull()
        {
            throw new ArgumentNullException();
        }
    }
}

namespace DCFApixels.DragonECS
{
    [Serializable]
    public class EcsRelationException : Exception
    {
        public EcsRelationException() { }
        public EcsRelationException(string message) : base(EcsConsts.EXCEPTION_MESSAGE_PREFIX + message) { }
        public EcsRelationException(string message, Exception inner) : base(EcsConsts.EXCEPTION_MESSAGE_PREFIX + message, inner) { }
        protected EcsRelationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
