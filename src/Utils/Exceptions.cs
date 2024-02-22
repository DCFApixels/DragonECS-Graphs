using System;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS.Relations.Internal
{
    internal static class Throw
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ArgumentNull()
        {
            throw new ArgumentNullException();
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UndefinedException()
        {
            throw new Exception();
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ArgumentOutOfRange()
        {
            throw new ArgumentOutOfRangeException();
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UndefinedRelationException()
        {
            throw new EcsRelationException();
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RelationAlreadyExists()
        {
            throw new EcsRelationException("This relation already exists.");
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
    }
}
