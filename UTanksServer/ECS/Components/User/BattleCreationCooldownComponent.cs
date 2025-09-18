using UTanksServer.ECS.ECSCore;

namespace UTanksServer.ECS.Components.User
{
    [TypeUid(449812176345098500)]
    public class BattleCreationCooldownComponent : ECSComponent
    {
        public static new long Id { get; set; }
        public static new System.Collections.Generic.List<System.Action> StaticOnChangeHandlers { get; set; }

        public long LastCreationUnixTimeSeconds;
    }
}
