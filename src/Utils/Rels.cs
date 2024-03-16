using DCFApixels.DragonECS.Graphs.Internal;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
    [Serializable]
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

        #region Other
        public override bool Equals(object obj) { throw new NotImplementedException(); }
        public override int GetHashCode() { throw new NotImplementedException(); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(StartEnd other) { return this == other; }
        public override string ToString() { return $"arc({start} -> {end})"; }
        #endregion
    }
    public readonly ref struct StartRelEnd
    {
        /// <summary>Start vertex entity ID.</summary>
        public readonly int start;
        /// <summary>Relation entity ID.</summary>
        public readonly int rel;
        /// <summary>End vertex entity ID.</summary>
        public readonly int end;

        #region Properties
        public bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return rel == 0 || (start == 0 && end == 0); }
        }
        public bool IsLoop
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return start == end; }
        }
        #endregion

        #region Constructor/Deconstruct
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StartRelEnd(int start, int rel, int end)
        {
            this.start = start;
            this.rel = rel;
            this.end = end;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out int start, out int rel, out int end)
        {
            start = this.start;
            rel = this.rel;
            end = this.end;
        }
        #endregion

        #region operators
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(StartRelEnd a, StartRelEnd b) { return a.start == b.start && a.rel == b.rel && a.end == b.end; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(StartRelEnd a, StartRelEnd b) { return a.start != b.start || a.rel != b.rel || a.end != b.end; }
        #endregion

        #region Other
        public override bool Equals(object obj) { throw new NotImplementedException(); }
        public override int GetHashCode() { throw new NotImplementedException(); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(StartRelEnd other) { return this == other; }
        public override string ToString() { return $"arc({start} --({rel})-> {end})"; }
        #endregion
    }
    public readonly ref struct RelEnd
    {
        /// <summary>Relation entity ID.</summary>
        public readonly int rel;
        /// <summary>End vertex entity ID.</summary>
        public readonly int end;

        #region Properties
        public bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return rel == 0; }
        }
        #endregion

        #region Constructor/Deconstruct
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RelEnd(int rel, int end)
        {
            this.rel = rel;
            this.end = end;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out int rel, out int end)
        {
            rel = this.rel;
            end = this.end;
        }
        #endregion

        #region operators
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(RelEnd a, RelEnd b) { return a.rel == b.rel && a.end == b.end; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(RelEnd a, RelEnd b) { return a.rel != b.rel || a.end != b.end; }
        #endregion

        #region Other
        public override bool Equals(object obj) { throw new NotImplementedException(); }
        public override int GetHashCode() { throw new NotImplementedException(); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(RelEnd other) { return this == other; }
        public override string ToString() { return $"arc(--({rel})-> {end})"; }
        #endregion
    }
}
