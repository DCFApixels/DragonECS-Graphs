#pragma warning disable IDE1006 // Стили именования
using DCFApixels.DragonECS.Utils;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
    [Serializable]
    public readonly struct RelationTargets : IEquatable<RelationTargets>
    {
        public static readonly RelationTargets Empty = new RelationTargets();

        public readonly int entity;
        public readonly int otherEntity;

        #region Properties
        public int left
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsInverted ? otherEntity : entity;
        }
        public int right
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsInverted ? entity : otherEntity;
        }

        public bool IsInverted
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => entity > otherEntity; // направление всегда с меньшего к большему
        }
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => entity == 0 && otherEntity == 0;
        }

        public RelationTargets Inverted
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new RelationTargets(otherEntity, entity);
        }
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal RelationTargets(int entity, int otherEntity)
        {
            this.entity = entity;
            this.otherEntity = otherEntity;
        }

        #region operators
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RelationTargets operator -(RelationTargets a) => a.Inverted;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(RelationTargets a, RelationTargets b) => a.entity == b.entity && a.otherEntity == b.otherEntity;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(RelationTargets a, RelationTargets b) => a.entity != b.entity || a.otherEntity != b.otherEntity;s
        #endregion

        #region Other
        public override bool Equals(object obj)
        {
            return obj is RelationTargets targets &&
                   entity == targets.entity &&
                   otherEntity == targets.otherEntity;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(RelationTargets other) => this == other;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => ~entity ^ otherEntity;
        public override string ToString()
        {
            return IsInverted ? 
                $"rel({entity} <- {otherEntity})" : 
                $"rel({entity} -> {otherEntity})";
        }
        #endregion
    }
}