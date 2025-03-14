#if DISABLE_DEBUG
#undef DEBUG
#endif
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS.Graphs.Internal
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
    internal readonly struct RelationInfo : IEquatable<RelationInfo>
    {
        public static readonly RelationInfo Empty = new RelationInfo();

        /// <summary>Start vertex entity ID.</summary>
        public readonly int start;
        /// <summary>End vertex entity ID.</summary>
        public readonly int end;

        #region Properties
        public bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return start == 0 && end == 0; }
        }
        public bool IsLoop
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return start == end; }
        }
        #endregion

        #region Constructor/Deconstruct
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal RelationInfo(int startEntity, int endEntity)
        {
            start = startEntity;
            end = endEntity;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out int start, out int end)
        {
            start = this.start;
            end = this.end;
        }
        #endregion

        #region operators
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(RelationInfo a, RelationInfo b) { return a.start == b.start && a.end == b.end; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(RelationInfo a, RelationInfo b) { return a.start != b.start || a.end != b.end; }
        #endregion

        #region Other
        public override bool Equals(object obj) { return obj is RelationInfo targets && targets == this; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(RelationInfo other) { return this == other; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            unchecked
            {
                uint endHash = (uint)end;
                endHash ^= endHash << 13;
                endHash ^= endHash >> 17;
                endHash ^= endHash << 5;
                return start ^ (int)endHash;
            }
        }
        public override string ToString() { return $"arc({start} -> {end})"; }
        #endregion
    }
}
