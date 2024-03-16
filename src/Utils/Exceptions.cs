using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS.Graphs.Internal
{
    internal static class Throw
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RelationAlreadyExists()
        {
            throw new EcsRelationException("This relation already exists.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UndefinedRelationException()
        {
            throw new EcsRelationException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ArgumentNull()
        {
            throw new ArgumentNullException();
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ArgumentOutOfRange()
        {
            throw new ArgumentOutOfRangeException($"index is less than 0 or is equal to or greater than Count.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UndefinedException()
        {
            throw new Exception();
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Exception(string message)
        {
            throw new Exception(message);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ArgumentException(string message)
        {
            throw new ArgumentException(message);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void KeyNotFound()
        {
            throw new KeyNotFoundException();
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
