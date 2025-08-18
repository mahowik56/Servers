using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using UTanksServer.Database;
using UTanksServer.Database.Databases;
using UTanksServer.ECS.Components;
using UTanksServer.ECS.Components.Battle;
using UTanksServer.ECS.Components.Battle.BattleComponents;
using UTanksServer.ECS.Components.Battle.BotComponent;
using UTanksServer.ECS.Components.Battle.Team;
using UTanksServer.ECS.ECSCore;
using UTanksServer.ECS.Events.Battle;
using UTanksServer.ECS.Events.User;
using UTanksServer.ECS.Templates.User;
using UTanksServer.Extensions;

namespace UTanksServer.Services
{
    public static class BotService
    {
        // Track spawned bot entities by their instance IDs
        private static readonly ConcurrentDictionary<long, ECSEntity> Bots = new();

        public static void AddBots(long battleId, long teamId, int count)
        {
            if (!ManagerScope.entityManager.EntityStorage.TryGetValue(battleId, out var battle))
            {
                Console.WriteLine("battle not found");
                return;
            }

            var teamsComp = battle.GetComponent(BattleTeamsComponent.Id) as BattleTeamsComponent;
            if (teamsComp == null || !teamsComp.teams.ContainsKey(teamId))
            {
                Console.WriteLine("team not found in battle");
                return;
            }

            if (!ManagerScope.entityManager.EntityStorage.TryGetValue(teamId, out var team))
            {
                Console.WriteLine("team not found");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                AddBotInternal(battle, team);
            }
        }

        private static void AddBotInternal(ECSEntity battle, ECSEntity team)
        {
            var userNetwork = new Network.Simple.Net.Server.User() { PlayerEntityId = Guid.NewGuid().GuidToLong() };
            var random = new Random();
            var userrow = new UserRow()
            {
                ActiveChatBanEndTime = 0,
                Clan = -1,
                Crystalls = 100000,
                Email = random.RandomString(10),
                EmailVerified = true,
                GarageJSONData = DatabaseQuery.NewbieAccountGarage,
                GlobalScore = 193000,
                HardwareId = random.RandomString(10),
                HardwareToken = random.RandomString(10),
                id = -1,
                Karma = 0,
                LastDatetimeGetDailyBonus = 0,
                LastIp = "127.0.0.1",
                Password = "c20ad4d76fe97759aa27a0c99bff6710",
                Rank = 14,
                RankScore = 1000,
                RegistrationDate = -1,
                TermlessBan = false,
                UserGroup = "admin",
                UserLocation = "en",
                Username = "bot_" + random.RandomString(8)
            };
            var entityUser = new UserTemplate().CreateEntity(userrow, userNetwork.PlayerEntityId);
            ManagerScope.eventManager.OnEventAdd(new UserLogged()
            {
                networkSocket = userNetwork,
                EntityOwnerId = userNetwork.PlayerEntityId,
                userRelogin = false,
                userEntity = entityUser,
                actionAfterLoggin = (entity) =>
                {
                    ManagerScope.eventManager.OnEventAdd(new EnterToBattleEvent()
                    {
                        BattleId = battle.instanceId,
                        TeamInstanceId = team.instanceId,
                        EntityOwnerId = entity.instanceId
                    });
                    Thread.Sleep(100);
                    ManagerScope.eventManager.OnEventAdd(new BattleLoadedEvent()
                    {
                        BattleId = battle.instanceId,
                        TeamInstanceId = team.instanceId,
                        EntityOwnerId = entity.instanceId
                    });
                    Thread.Sleep(100);
                    entity.AddComponent(new AutoSmokyComponent(5f).SetGlobalComponentGroup());
                    entity.AddComponent(new AutoMoveComponent(1f).SetGlobalComponentGroup());
                    Bots[entity.instanceId] = entity;
                }
            });
        }

        public static bool RemoveBot(long botId)
        {
            if (Bots.TryRemove(botId, out var entity))
            {
                if (entity.HasComponent(BattleOwnerComponent.Id))
                {
                    var owner = entity.GetComponent<BattleOwnerComponent>();
                    ManagerScope.eventManager.OnEventAdd(new LeaveFromBattleEvent()
                    {
                        BattleId = owner.BattleInstanceId,
                        TeamInstanceId = entity.GetComponent<TeamComponent>().instanceId,
                        EntityOwnerId = entity.instanceId
                    });
                }
                ManagerScope.entityManager.OnRemoveEntity(entity);
                return true;
            }
            return false;
        }

        public static long[] ListBots()
        {
            return Bots.Keys.ToArray();
        }
    }
}
