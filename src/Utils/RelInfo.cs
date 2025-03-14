#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Graphs.Internal;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
    public readonly ref struct StartEnd
    {
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
        internal StartEnd(RelationInfo relInfo)
        {
            start = relInfo.start;
            end = relInfo.end;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal StartEnd(int startEntity, int endEntity)
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
        public static bool operator ==(StartEnd a, StartEnd b) { return a.start == b.start && a.end == b.end; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(StartEnd a, StartEnd b) { return a.start != b.start || a.end != b.end; }
        #endregion

        #region Methods
        public int GetOpposite(int entityID)
        {
#if DEBUG
            if (entityID != start && entityID != end) { Throw.UndefinedException(); }
#endif
            //return entityID > end ? end : start;
            return entityID == end ? start : end;
        }
        #endregion

        #region Other
        public override int GetHashCode() { throw new NotSupportedException(); }
        public override bool Equals(object obj) { throw new NotSupportedException(); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(StartEnd other) { return this == other; }
        public override string ToString() { return $"rel({start} -> {end})"; }
        #endregion
    }
}