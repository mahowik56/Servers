﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UTanksServer.ECS.ECSCore;
using UTanksServer.ECS.Types.Battle.Bonus;

namespace UTanksServer.ECS.Components.Battle.BattleComponents
{

    [TypeUid(220438189710212500)]
    public class BattleDropRegionsComponent : ECSComponent
    {
        static public new long Id { get; set; }
        static public new System.Collections.Generic.List<System.Action> StaticOnChangeHandlers { get; set; }
        //long is entity id
        public ConcurrentDictionary<BonusType, List<long>> dropRegions = new ConcurrentDictionary<BonusType, List<long>>();
    }
}
