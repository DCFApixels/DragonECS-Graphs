using System;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public readonly ref struct StartRelEnd
    {
        /// <summary>Start vertex entity ID.</summary>
        public readonly int start;
        /// <summary>Relation entity ID.</summary>
        public readonly int rel;
        /// <summary>End vertex entity ID.</summary>
        public readonly int end;
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
    }
    public readonly ref struct RelEnd
    {
        /// <summary>Relation entity ID.</summary>
        public readonly int rel;
        /// <summary>End vertex entity ID.</summary>
        public readonly int end;
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
    }
}
