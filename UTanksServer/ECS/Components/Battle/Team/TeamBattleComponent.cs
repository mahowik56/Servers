﻿using UTanksServer.Core.Protocol;
using UTanksServer.ECS.ECSCore;

namespace UTanksServer.ECS.Components.Battle.Team
{
    [TypeUid(-7175336783650365377)]
    public class TeamBattleComponent : ECSComponent
    {
        static public new long Id { get; set; }
        static public new System.Collections.Generic.List<System.Action> StaticOnChangeHandlers { get; set; }
    }
}