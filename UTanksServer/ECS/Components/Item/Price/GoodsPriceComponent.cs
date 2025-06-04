using UTanksServer.Core.Protocol;
using UTanksServer.ECS.ECSCore;

namespace UTanksServer.ECS.Components.Item.Price
{
    [TypeUid(1453891891716L)]
    public class GoodsPriceComponent : ECSComponent
    {
        static public new long Id { get; set; }
        static public new System.Collections.Generic.List<System.Action> StaticOnChangeHandlers { get; set; }
        public string Currency { get; set; }
        public double Price { get; set; }
    }
}
