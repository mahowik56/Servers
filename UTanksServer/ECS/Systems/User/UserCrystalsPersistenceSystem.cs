using System;
using System.Collections.Generic;
using UTanksServer.ECS.Components;
using UTanksServer.ECS.Components.User;
using UTanksServer.ECS.ECSCore;
using UTanksServer.Extensions;
using UTanksServer.Database.Databases;

namespace UTanksServer.ECS.Systems.User
{
    public class UserCrystalsPersistenceSystem : ECSSystem
    {
        public override bool HasInterest(ECSEntity entity)
        {
            throw new NotImplementedException();
        }

        public override void Initialize(ECSSystemManager SystemManager)
        {
            ComponentsOnChangeCallbacks.Add(UserCrystalsComponent.Id, new List<Action<ECSEntity, ECSComponent>>()
            {
                (entity, component) =>
                {
                    var crystals = component as UserCrystalsComponent;
                    if (crystals == null)
                        return;
                    var username = entity.GetComponent<UsernameComponent>().Username;
                    _ = UserDatabase.Users.UpdateCrystallsAsync(username, crystals.UserCrystals);
                }
            });
        }

        public override void Operation(ECSEntity entity, ECSComponent Component)
        {
            throw new NotImplementedException();
        }

        public override ConcurrentDictionaryEx<long, int> ReturnInterestedComponentsList()
        {
            return new ConcurrentDictionaryEx<long, int>() { IKeys = { }, IValues = { } }.Upd();
        }

        public override ConcurrentDictionaryEx<long, int> ReturnInterestedEventsList()
        {
            return new ConcurrentDictionaryEx<long, int>() { IKeys = { }, IValues = { } }.Upd();
        }

        public override void Run(long[] entities)
        {
            throw new NotImplementedException();
        }

        public override bool UpdateInterestedList(List<long> ComponentsId)
        {
            throw new NotImplementedException();
        }
    }
}
