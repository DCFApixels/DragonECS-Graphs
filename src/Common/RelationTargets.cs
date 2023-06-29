namespace DCFApixels.DragonECS
{
    public readonly struct RelationTargets
    {
        public static readonly RelationTargets Empty = new RelationTargets();
        public readonly int entity;
        public readonly int otherEntity;

        public bool IsEmpty => entity == 0 && otherEntity == 0;

        public RelationTargets(int entity, int otherEntity)
        {
            this.entity = entity;
            this.otherEntity = otherEntity;
        }

        public override bool Equals(object obj)
        {
            return obj is RelationTargets targets &&
                   entity == targets.entity &&
                   otherEntity == targets.otherEntity;
        }
        public override int GetHashCode() => ~entity ^ otherEntity;
        public override string ToString() => $"rel({entity}, {otherEntity})";


        public static bool operator ==(RelationTargets a, RelationTargets b) => (a.entity == b.entity && a.otherEntity == b.otherEntity);
        public static bool operator !=(RelationTargets a, RelationTargets b) => a.entity != b.entity || a.otherEntity != b.otherEntity;
    }
}