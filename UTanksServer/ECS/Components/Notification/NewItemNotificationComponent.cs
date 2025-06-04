﻿using System;
using UTanksServer.Core.Protocol;
using UTanksServer.ECS.ECSCore;

namespace UTanksServer.ECS.Components
{
    [TypeUid(1481177510866L)]
    public class NewItemNotificationComponent : ECSComponent
    {
        static public new long Id { get; set; }
        static public new System.Collections.Generic.List<System.Action> StaticOnChangeHandlers { get; set; }
        public NewItemNotificationComponent() { }
        public NewItemNotificationComponent(ECSEntity item, int amount)
        {
            Item = item ?? throw new ArgumentNullException(nameof(item));
            Amount = amount;
        }

        public ECSEntity Item { get; set; }

        public int Amount { get; set; }
    }
}
