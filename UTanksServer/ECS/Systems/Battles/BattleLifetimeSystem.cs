using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core;
using Newtonsoft.Json;
using UTanksServer.Database.Databases;
using UTanksServer.Database.Databases.ServerTables;
using UTanksServer.ECS.Components;
using UTanksServer.ECS.Components.Battle;
using UTanksServer.ECS.Components.Battle.BattleComponents;
using UTanksServer.ECS.Components.Battle.Round;
using UTanksServer.ECS.Components.Battle.Team;
using UTanksServer.ECS.Components.ECSComponents;
using UTanksServer.ECS.Components.User;
using UTanksServer.ECS.ECSCore;
using UTanksServer.ECS.Events.Battle;
using UTanksServer.ECS.Events.ECSEvents;
using UTanksServer.ECS.Systems.NetworkUpdatingSystems;
using UTanksServer.ECS.Templates.Battle;
using UTanksServer.Extensions;

namespace UTanksServer.ECS.Systems.Battles
{
    public class BattleLifetimeSystem : ECSSystem
    {
        public override bool HasInterest(ECSEntity entity)
        {
            throw new NotImplementedException();
        }

        public override void Initialize(ECSSystemManager SystemManager)
        {
            SystemEventHandler.Add(CreateBattleEvent.Id, new List<Func<ECSEvent, object>>() {
                (Event) => {
                    CreateBattle(Event as CreateBattleEvent);
                    Event.eventWatcher.Watchers--;
                    return null;
                }
            });
            SystemEventHandler.Add(RemoveBattleEvent.Id, new List<Func<ECSEvent, object>>() {
                (Event) => {
                    RemoveBattle(Event as RemoveBattleEvent);
                    Event.eventWatcher.Watchers--;
                    return null;
                }
            });
            SystemEventHandler.Add(BattleStartEvent.Id, new List<Func<ECSEvent, object>>() {
                (Event) => {
                    BattleStart(Event as BattleStartEvent);
                    Event.eventWatcher.Watchers--;
                    return null;
                }
            });
            SystemEventHandler.Add(BattleEndEvent.Id, new List<Func<ECSEvent, object>>() {
                (Event) => {
                    BattleEnd(Event as BattleEndEvent);
                    Event.eventWatcher.Watchers--;
                    return null;
                }
            });
            //this.Enabled = true;
            this.DelayRunMilliseconds = 30000;
        }

        public static void RemoveBattle(RemoveBattleEvent removeBattleEvent)
        {
            var battleEntity = ManagerScope.entityManager.EntityStorage[removeBattleEvent.BattleId];
            var battleDropZones = battleEntity.GetComponent<BattleDropRegionsComponent>(BattleDropRegionsComponent.Id);
            var battleGameplay = battleEntity.GetComponent<BattleGameplayEntitiesComponent>(BattleGameplayEntitiesComponent.Id);
            foreach(var DropZoneList in battleDropZones.dropRegions.Values)
            {
                foreach(var dropZone in DropZoneList)
                {
                    ManagerScope.entityManager.OnRemoveEntity(ManagerScope.entityManager.EntityStorage[dropZone]);
                }
            }
            foreach (var gameplayEntity in battleGameplay.GameplayEntities)
            {
                ManagerScope.entityManager.OnRemoveEntity(ManagerScope.entityManager.EntityStorage[gameplayEntity]);
            }
            ManagerScope.entityManager.OnRemoveEntity(battleEntity);
            if (ServerDatabase.Battles != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ServerDatabase.Battles.Remove(battleEntity.instanceId);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Failed to remove battle {battleEntity.instanceId} metadata: {ex.Message}", "BattleDB");
                    }
                });
            }
        }

        public static void CreateBattle(CreateBattleEvent createBattleEvent)
        {
            if (!ManagerScope.entityManager.EntityStorage.TryGetValue(createBattleEvent.EntityOwnerId, out var ownerEntity))
            {
                Logger.LogWarn($"Battle creation request ignored. Owner entity '{createBattleEvent.EntityOwnerId}' not found.", "Battle");
                return;
            }

            var cooldown = ownerEntity.TryGetComponent<BattleCreationCooldownComponent>();
            var nowSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (cooldown != null && cooldown.LastCreationUnixTimeSeconds > 0)
            {
                var elapsed = nowSeconds - cooldown.LastCreationUnixTimeSeconds;
                if (elapsed < 30)
                {
                    Logger.LogWarn($"Player {ownerEntity.instanceId} attempted to create a battle before cooldown expired. {30 - elapsed} seconds remaining.", "Battle");
                    return;
                }
            }

            var createdEntities = new BattleTemplate().CreateBattleEntities(createBattleEvent);
            if (createdEntities.Count == 0)
                return;

            ECSEntity battleEntity = null;
            foreach (var entity in createdEntities)
            {
                ManagerScope.entityManager.OnAddNewEntity(entity);
                if (entity.HasComponent(BattleComponent.Id))
                {
                    battleEntity = entity;
                    entity.GetComponent<BattleComponent>(BattleComponent.Id).MarkAsChanged();
                }
            }

            if (battleEntity == null)
                return;

            ownerEntity.AddOrChangeComponent(new BattleCreationCooldownComponent()
            {
                LastCreationUnixTimeSeconds = nowSeconds
            });

            SaveBattleToDatabase(battleEntity, createBattleEvent, nowSeconds);
        }

        static void SaveBattleToDatabase(ECSEntity battleEntity, CreateBattleEvent createBattleEvent, long createdAt)
        {
            if (ServerDatabase.Battles == null)
                return;

            var battleComponent = battleEntity.GetComponent<BattleComponent>(BattleComponent.Id);
            var parametersPayload = new
            {
                createBattleEvent.BattleCustomName,
                createBattleEvent.BattleRealName,
                createBattleEvent.GameMapGroupName,
                createBattleEvent.MapPath,
                createBattleEvent.MapModel,
                createBattleEvent.MapConfigPath,
                createBattleEvent.MaxPlayers,
                createBattleEvent.BattleTimeMinutes,
                createBattleEvent.BattleWinGoalValue,
                createBattleEvent.MinimalPlayerRankValue,
                createBattleEvent.MaximalPlayerRankValue,
                createBattleEvent.WeatherMode,
                createBattleEvent.TimeMode,
                createBattleEvent.BattleMode,
                createBattleEvent.ListOfAcceptedConfigPathWeapon,
                createBattleEvent.ListOfAcceptedConfigPathHull,
                createBattleEvent.isProBattle,
                createBattleEvent.isClosedBattle,
                createBattleEvent.isParkourBattle,
                createBattleEvent.isTournamentBattle,
                createBattleEvent.enableUnlimitedUserSupply,
                createBattleEvent.enableSuperDrop,
                createBattleEvent.enableAutoPeaceOnSuperDrop,
                createBattleEvent.enableMicroUpgrade,
                createBattleEvent.enableDressingUp,
                createBattleEvent.dressingUpTimeoutSeconds,
                createBattleEvent.enableSupplyDrop,
                createBattleEvent.enableCrystalDrop,
                createBattleEvent.enablePlayerSupplies,
                createBattleEvent.enablePlayerAutoBalance,
                createBattleEvent.enableBattleAutoEnding,
                createBattleEvent.enableTeamKilling,
                createBattleEvent.enableResists,
                createBattleEvent.enableModules,
                createBattleEvent.enablePlayerSupplySeparation,
                createBattleEvent.enableSupplyCooldown,
                createBattleEvent.LuminosityStrength,
                createBattleEvent.DamageScalingCoeficient,
                createBattleEvent.HealthScalingCoeficient,
                createBattleEvent.GravityScaling,
                createBattleEvent.MassScaling,
                createBattleEvent.isTestBoxBattle,
                createBattleEvent.isCheatersBattle
            };

            var dbRow = new BattleDbRow
            {
                BattleId = battleEntity.instanceId,
                CustomName = battleComponent.BattleCustomName,
                RealName = battleComponent.BattleRealName,
                MapGroup = createBattleEvent.GameMapGroupName,
                MapPath = battleComponent.MapPath,
                Mode = battleComponent.BattleMode,
                OwnerEntityId = createBattleEvent.EntityOwnerId,
                MaxPlayers = battleComponent.MaxPlayers,
                MinRank = battleComponent.MinimalPlayerRankValue,
                MaxRank = battleComponent.MaximalPlayerRankValue,
                CreatedAt = createdAt,
                ParametersJson = JsonConvert.SerializeObject(parametersPayload)
            };

            _ = Task.Run(async () =>
            {
                try
                {
                    await ServerDatabase.Battles.Upsert(dbRow);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to persist battle {battleEntity.instanceId} metadata: {ex.Message}", "BattleDB");
                }
            });
        }

        public static void BattleEnd(BattleEndEvent battleEndEvent)
        {
            var roundGroup = new RoundGroupComponent();
            var battleEntity = ManagerScope.entityManager.EntityStorage[battleEndEvent.BattleEntity];
            var roundRestarting = new RoundRestartingStateComponent(9f).SetGlobalComponentGroup().AddComponentGroup(roundGroup) as RoundRestartingStateComponent;
            
            var fund = battleEntity.GetComponent<RoundFundComponent>();
            if(fund.Fund != 0)
            {
                var players = battleEntity.GetComponent<BattlePlayersComponent>().players;
                var WinnerTeam = battleEndEvent.TeamWinnerInstanceId;
                var sortedTeams = battleEntity.GetComponent<BattleTeamsComponent>().teams.Values.OrderByDescending(x => x.GoalScore).ToList();
                if (WinnerTeam == 0)
                {
                    WinnerTeam = sortedTeams.First().instanceId;
                }
                var winnerFund = 0f;
                var loserFund = 0f;
                if(Math.Round(fund.Fund) != 0)
                {
                    var fundFactor = fund.Fund / sortedTeams.Count;
                    if (battleEntity.HasComponent(DMComponent.Id))
                    {
                        winnerFund = fund.Fund;
                    }
                    else
                    {
                        winnerFund = fundFactor + (fundFactor * (sortedTeams.Count) * GlobalGameDataConfig.SelectableMap.selectableMaps.WinnerFundAdvantage);
                        loserFund = fundFactor - (fundFactor * GlobalGameDataConfig.SelectableMap.selectableMaps.WinnerFundAdvantage);
                    }
                }
                for (int i = 0; i < sortedTeams.Count; i++)
                {
                    if (i == 0)
                    {
                        var winners = players.Where(x => x.Value.instanceId == WinnerTeam).ToList().Select(x => x.Key);
                        var winnersScoreSum = 0f;
                        winners.ForEach(x => winnersScoreSum += x.GetComponent<RoundUserStatisticsComponent>().ScoreWithoutBonuses);
                        winners.ForEach(x => {
                            x.GetComponent<UserCrystalsComponent>().UserCrystals += (int)(winnerFund * (x.GetComponent<RoundUserStatisticsComponent>().ScoreWithoutBonuses / winnersScoreSum));
                            roundRestarting.rewardStorage.Add(x.instanceId, (int)(winnerFund * (x.GetComponent<RoundUserStatisticsComponent>().ScoreWithoutBonuses / winnersScoreSum)));
                            x.GetComponent<UserCrystalsComponent>().MarkAsChanged();
                        });
                    }
                    else
                    {
                        var losers = players.Where(x => x.Value.instanceId == sortedTeams[i].instanceId).ToList().Select(x => x.Key);
                        var losersScoreSum = 0f;
                        losers.ForEach(x => losersScoreSum += x.GetComponent<RoundUserStatisticsComponent>().ScoreWithoutBonuses);
                        losers.ForEach(x => {
                            x.GetComponent<UserCrystalsComponent>().UserCrystals += (int)(loserFund * (x.GetComponent<RoundUserStatisticsComponent>().ScoreWithoutBonuses / losersScoreSum));
                            roundRestarting.rewardStorage.Add(x.instanceId, (int)(winnerFund * (x.GetComponent<RoundUserStatisticsComponent>().ScoreWithoutBonuses / losersScoreSum)));
                            x.GetComponent<UserCrystalsComponent>().MarkAsChanged();
                        });
                        //need append calculation of losers SumScore, and use it for calc fund of losers team, this code calculation correctly and fair work only for team vs team and no more teams
                    }
                }
            }
            else
            {
                var players = battleEntity.GetComponent<BattlePlayersComponent>().players;
                players.ForEach(x => roundRestarting.rewardStorage.Add(x.Key.instanceId, 0));
            }
            
            battleEntity.AddComponent(roundRestarting);
            battleEntity.GetComponent<BattleSimpleInfoComponent>().MarkAsChanged();
        }

        public static void BattleStart(BattleStartEvent battleStartEvent)
        {
            var battleEntity = ManagerScope.entityManager.EntityStorage[battleStartEvent.BattleId];
            var roundGroup = new RoundGroupComponent();
            if (battleEntity.HasComponent<RoundTimerComponent>())
            {
                battleEntity.GetComponent<RoundTimerComponent>().timerAwait = 0;
                battleEntity.TryRemoveComponent(RoundTimerComponent.Id);
            }
            battleEntity.AddComponent(new RoundTimerComponent(battleEntity.GetComponent<BattleComponent>().BattleTimeMinutes).SetGlobalComponentGroup().AddComponentGroup(roundGroup));
            var battleGroup = new BattleGroupComponent();
            battleEntity.GetComponent<BattlePlayersComponent>().players.Keys.ForEach(x => {
                var teamComp = x.GetComponent<TeamComponent>();
                BattlesSystem.RespawnTank(battleEntity, null, x, teamComp, true);
                x.RemoveComponent(RoundUserStatisticsComponent.Id);
                x.AddComponent(new RoundUserStatisticsComponent((battleEntity.GetComponent(BattlePlayersComponent.Id) as BattlePlayersComponent).players.Values.Where(x => x == teamComp).ToList().Count - 1, 0, 0, 0, 0, (x.GetComponent(UsernameComponent.Id) as UsernameComponent).Username, (x.GetComponent(UserRankComponent.Id) as UserRankComponent).Rank).SetGlobalComponentGroup().AddComponentGroup(battleGroup) as RoundUserStatisticsComponent);
            });
            battleEntity.GetComponent<RoundUsersStatisticsStorageComponent>().roundUserStatisticsComponents.Clear();
            var fund = battleEntity.GetComponent<RoundFundComponent>();
            battleEntity.RemoveComponent(RoundFundComponent.Id);
            battleEntity.AddComponent(new RoundFundComponent(fund.TargetSectionCoef).SetGlobalComponentGroup().AddComponentGroup(roundGroup));
            battleEntity.GetComponent<BattleTeamsComponent>().teams.Values.ForEach(x => x.GoalScore = 0);
            battleEntity.GetComponent<BattleScoreComponent>().MarkAsChanged();
            BattleInfoUpdaterSystem.BattleSimpleInfoUpdater(battleEntity);

            var battleDataList = new List<string>() { EntitySerialization.BuildFullSerializedEntityWithGDAP(battleEntity, battleEntity) };
            battleEntity.GetComponent<BattlePlayersComponent>().players.Keys.ForEach(x => {
                UpdateEntitiesEvent updateEntitiesEvent = new UpdateEntitiesEvent()
                {
                    EntityIdRecipient = x.instanceId,
                    Entities = battleDataList
                };
                Func<Task> asyncUpd = async () =>
                {
                    await Task.Run(() => {
                        var userSocket = (x.GetComponent(UserSocketComponent.Id) as UserSocketComponent);
                        if (updateEntitiesEvent.Entities.Count > 0)
                            userSocket.Socket.emit(updateEntitiesEvent.CachePackToNetworkPacket());
                    });
                };
                asyncUpd();
            });
        }

        public override void Operation(ECSEntity entity, ECSComponent Component)
        {
            throw new NotImplementedException();
        }

        public override ConcurrentDictionaryEx<long, int> ReturnInterestedComponentsList()
        {
            return new ConcurrentDictionaryEx<long, int>()
            {
                IKeys = {
                    BattleComponent.Id
                },
                IValues = { 0 }
            }.Upd();
        }

        public override ConcurrentDictionaryEx<long, int> ReturnInterestedEventsList()
        {
            return new ConcurrentDictionaryEx<long, int>()
            {
                IKeys = {
                    CreateBattleEvent.Id,
                    RemoveBattleEvent.Id,
                    BattleEndEvent.Id,
                    BattleStartEvent.Id
                },
                IValues = { 0, 0, 0, 0 }
            }.Upd();
        }

        public override void Run(long[] entities)
        {
            foreach(var entityBattleId in entities)
            {
                var battleEntity = ManagerScope.entityManager.EntityStorage[entityBattleId];
                var battlePlayers = battleEntity.GetComponent<BattlePlayersComponent>(BattlePlayersComponent.Id);
                if(battlePlayers.players.Count == 0 && !battleEntity.HasComponent(TimerSelfDestructionComponent.Id))
                {
                    battleEntity.AddComponent(new TimerSelfDestructionComponent(30, 
                    (entity) =>
                    {
                        var battlePlayers = entity.GetComponent<BattlePlayersComponent>(BattlePlayersComponent.Id);
                        return battlePlayers.players.Count == 0;
                    },
                    (entity)=>
                    {
                        ManagerScope.eventManager.OnEventAdd(new RemoveBattleEvent() {BattleId = entity.instanceId });
                    }
                    ));
                }
            }
            this.LastEndExecutionTimestamp = DateTime.Now.Ticks;
            this.InWork = false;
        }

        public override void UpdateEventWatcher(ECSEvent eCSEvent)
        {
            throw new NotImplementedException();
        }

        public override bool UpdateInterestedList(List<long> ComponentsId)
        {
            throw new NotImplementedException();
        }
    }
}
