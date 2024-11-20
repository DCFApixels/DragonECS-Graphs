﻿using System.Diagnostics;

namespace DCFApixels.DragonECS
{
    /// <summary> EcsWrold for store regular game entities. </summary>
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.WORLDS_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "Inherits EcsWorld without extending its functionality and is used for specific injections. Can be used as argument to EcsWorld.CreateGraph(new " + nameof(EcsGraphWorld) + "()) and to store relation entity.")]
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    [MetaID("ECC4CF479301897718600925B00A7DB4")]
    public sealed class EcsGraphWorld : EcsWorld, IInjectionUnit
    {
        public EcsGraphWorld(EcsWorldConfig config, short worldID = -1) : base(config, worldID) { }
        public EcsGraphWorld(IConfigContainer configs = null, short worldID = -1) : base(configs, worldID) { }
        void IInjectionUnit.InitInjectionNode(InjectionGraph nodes) { nodes.AddNode(this); }
    }
}