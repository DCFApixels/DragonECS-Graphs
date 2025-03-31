using System.Diagnostics;

namespace DCFApixels.DragonECS
{
    /// <summary> EcsWrold for store regular game entities. </summary>
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.WORLDS_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "Inherits EcsWorld without extending its functionality and is used for specific injections. Can be used as argument to EcsWorld.CreateGraph(new " + nameof(EcsGraphWorld) + "()) and to store relation entity.")]
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    [MetaID("DragonECS_ECC4CF479301897718600925B00A7DB4")]
    public sealed class EcsGraphWorld : EcsWorld, IInjectionUnit, IInjectionBlock
    {
        private const string DEFAULT_NAME = "Graph";
        public EcsGraphWorld() : base(default(EcsWorldConfig), DEFAULT_NAME) { }
        public EcsGraphWorld(EcsWorldConfig config = null, string name = null, short worldID = -1) : base(config, name == null ? DEFAULT_NAME : name, worldID) { }
        public EcsGraphWorld(IConfigContainer configs, string name = null, short worldID = -1) : base(configs, name == null ? DEFAULT_NAME : name, worldID) { }
        void IInjectionUnit.InitInjectionNode(InjectionGraph nodes)
        {
            nodes.AddNode(this);
            nodes.AddNode<EntityGraph>();
        }
        public void InjectTo(Injector inj)
        {
            inj.Inject(this.GetGraph());
        }
    }
}