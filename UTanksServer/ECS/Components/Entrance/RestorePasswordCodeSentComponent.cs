﻿using UTanksServer.Core.Protocol;
using UTanksServer.ECS.ECSCore;

namespace UTanksServer.ECS.Components.Entrance
{
    [TypeUid(1479198715562L)]
    public sealed class RestorePasswordCodeSentComponent : ECSComponent
    {
        static public new long Id { get; set; }
        static public new System.Collections.Generic.List<System.Action> StaticOnChangeHandlers { get; set; }
        public RestorePasswordCodeSentComponent() { }
        public RestorePasswordCodeSentComponent(string email)
        {
            Email = email;
        }

        public string Email { get; set; }
    }
}
