using System;
using UTanksServer.ECS.Components.Battle.Location;
using UTanksServer.ECS.ECSCore;
using UTanksServer.ECS.Events.Battle.TankEvents;
using UTanksServer.Network.NetworkEvents.FastGameEvents;
using UTanksServer.ECS.Types.Battle;

namespace UTanksServer.ECS.Components.Battle.BotComponent
{
    [TypeUid(223899603046642000)]
    public class AutoMoveComponent : TimerComponent
    {
        static public new long Id { get; set; }
        static public new System.Collections.Generic.List<System.Action> StaticOnChangeHandlers { get; set; }
        private readonly Random random = new Random();
        public float MovePeriod;
        public AutoMoveComponent() { }
        public AutoMoveComponent(float period)
        {
            MovePeriod = period;
            onEnd = (entity, timerComp) =>
            {
                try
                {
                    var world = entity.GetComponent<WorldPositionComponent>().WorldPoint.Position;
                    world.x += (float)(random.NextDouble() * 2 - 1);
                    world.z += (float)(random.NextDouble() * 2 - 1);
                    var moveEvent = new MoveCommandEvent()
                    {
                        EntityOwnerId = entity.instanceId,
                        position = world,
                        velocity = new Vector3S(),
                        angularVelocity = new Vector3S(),
                        rotation = new QuaternionS(),
                        turretRotation = new QuaternionS(),
                        WeaponRotation = 0,
                        TankMoveControl = 0,
                        TankTurnControl = 0,
                        WeaponRotationControl = 0,
                        ClientTime = 0
                    };
                    moveEvent.cachedRawEvent = new RawMovementEvent()
                    {
                        packetId = 0,
                        PlayerEntityId = entity.instanceId,
                        position = moveEvent.position,
                        velocity = moveEvent.velocity,
                        angularVelocity = moveEvent.angularVelocity,
                        rotation = moveEvent.rotation,
                        turretRotation = moveEvent.turretRotation,
                        WeaponRotation = moveEvent.WeaponRotation,
                        TankMoveControl = moveEvent.TankMoveControl,
                        TankTurnControl = moveEvent.TankTurnControl,
                        WeaponRotationControl = moveEvent.WeaponRotationControl,
                        ClientTime = moveEvent.ClientTime
                    };
                    ManagerScope.eventManager.OnEventAdd(moveEvent);
                }
                catch { }
            };
        }
        public override void OnAdded(ECSEntity entity)
        {
            base.OnAdded(entity);
            TimerStart(MovePeriod, ownerEntity, true, true);
        }
    }
}
