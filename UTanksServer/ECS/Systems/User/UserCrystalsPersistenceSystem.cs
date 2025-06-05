using System;
using System.Collections.Generic;
using UTanksServer.ECS.Components;
using UTanksServer.ECS.Components.User;
using UTanksServer.ECS.ECSCore;
using UTanksServer.Extensions;
using UTanksServer.Database.Databases;
using UTanksServer;
using Newtonsoft.Json;

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

            ComponentsOnChangeCallbacks.Add(UserRankComponent.Id, new List<Action<ECSEntity, ECSComponent>>()
            {
                (entity, component) =>
                {
                    if (component is not UserRankComponent rank)
                        return;
                    var username = entity.GetComponent<UsernameComponent>().Username;
                    _ = UserDatabase.Users.UpdateRankAsync(username, rank.Rank);
                }
            });

            ComponentsOnChangeCallbacks.Add(UserScoreComponent.Id, new List<Action<ECSEntity, ECSComponent>>()
            {
                (entity, component) =>
                {
                    if (component is not UserScoreComponent score)
                        return;
                    var username = entity.GetComponent<UsernameComponent>().Username;
                    _ = UserDatabase.Users.UpdateScoreAsync(username, score.GlobalScore, score.RankScore);
                }
            });

            ComponentsOnChangeCallbacks.Add(UserLocationComponent.Id, new List<Action<ECSEntity, ECSComponent>>()
            {
                (entity, component) =>
                {
                    if (component is not UserLocationComponent location)
                        return;
                    var username = entity.GetComponent<UsernameComponent>().Username;
                    _ = UserDatabase.Users.UpdateLocationAsync(username, location.UserLocation);
                }
            });

            ComponentsOnChangeCallbacks.Add(UserKarmaComponent.Id, new List<Action<ECSEntity, ECSComponent>>()
            {
                (entity, component) =>
                {
                    if (component is not UserKarmaComponent karma)
                        return;
                    var username = entity.GetComponent<UsernameComponent>().Username;
                    _ = UserDatabase.Users.UpdateKarmaAsync(username, karma.UserKarma);
                }
            });

            ComponentsOnChangeCallbacks.Add(UserGarageDBComponent.Id, new List<Action<ECSEntity, ECSComponent>>()
            {
                (entity, component) =>
                {
                    if (component is not UserGarageDBComponent garage)
                        return;
                    var username = entity.GetComponent<UsernameComponent>().Username;
                    // The component contains structured data; store JSON
                    var json = JsonConvert.SerializeObject(garage);
                    _ = UserDatabase.Users.UpdateGarageAsync(username, json);
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
