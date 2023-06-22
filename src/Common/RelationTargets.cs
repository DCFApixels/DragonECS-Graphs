namespace DCFApixels.DragonECS
{
    public readonly struct RelationTargets
    {
        public static readonly RelationTargets Empty = new RelationTargets();
        public readonly int entity;
        public readonly int otherEntity;
        public RelationTargets(int entity, int otherEntity)
        {
            this.entity = entity;
            this.otherEntity = otherEntity;
        }
        public override string ToString() => $"rel({entity}, {otherEntity})";
    }
}