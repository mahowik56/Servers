﻿using System.Numerics;
using UTanksServer.Core.Protocol;
using UTanksServer.ECS.ECSCore;

namespace UTanksServer.ECS.Components.Battle.Location
{
    /// <summary>
    /// Bonus rotation.
    /// </summary>
    [TypeUid(-1853333282151870933)]
    public class RotationComponent : ECSComponent
    {
        static public new long Id { get; set; }
        static public new System.Collections.Generic.List<System.Action> StaticOnChangeHandlers { get; set; }
        public RotationComponent() { }
        public RotationComponent(Vector3 rotationEuler)
        {
            RotationEuler = rotationEuler;
        }
        
        public Vector3 RotationEuler { get; set; }
    }
}