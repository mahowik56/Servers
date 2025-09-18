using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Threading.Tasks;
using Core;
using UTanksServer.Services;

namespace UTanksServer.Database.Databases.ServerTables
{
    public class Battles
    {
        public async Task<Battles> Init()
        {
            using (var request = new SQLiteCommand(
                "CREATE TABLE IF NOT EXISTS `battles` (" +
                "  `battle_id` INTEGER PRIMARY KEY," +
                "  `custom_name` TEXT NOT NULL," +
                "  `real_name` TEXT NOT NULL," +
                "  `map_group` TEXT NOT NULL," +
                "  `map_path` TEXT NOT NULL," +
                "  `mode` TEXT NOT NULL," +
                "  `owner_entity_id` INTEGER NOT NULL," +
                "  `max_players` INTEGER NOT NULL," +
                "  `min_rank` INTEGER NOT NULL," +
                "  `max_rank` INTEGER NOT NULL," +
                "  `created_at` INTEGER NOT NULL," +
                "  `parameters_json` TEXT NOT NULL" +
                ");",
                ServerDatabase.Connection))
            {
                ServerMonitor.RegisterDbWrite();
                await request.ExecuteNonQueryAsync();
            }

            Logger.Log("Table 'ServerDatabase.battles' initilized", "init");
            return this;
        }

        public async Task<bool> Upsert(BattleDbRow data)
        {
            using (var request = new SQLiteCommand(
                "INSERT OR REPLACE INTO battles (battle_id, custom_name, real_name, map_group, map_path, mode, owner_entity_id, max_players, min_rank, max_rank, created_at, parameters_json) " +
                "VALUES (@battleId, @customName, @realName, @mapGroup, @mapPath, @mode, @ownerEntityId, @maxPlayers, @minRank, @maxRank, @createdAt, @parametersJson);",
                ServerDatabase.Connection))
            {
                request.Parameters.AddWithValue("@battleId", data.BattleId);
                request.Parameters.AddWithValue("@customName", data.CustomName);
                request.Parameters.AddWithValue("@realName", data.RealName);
                request.Parameters.AddWithValue("@mapGroup", data.MapGroup);
                request.Parameters.AddWithValue("@mapPath", data.MapPath);
                request.Parameters.AddWithValue("@mode", data.Mode);
                request.Parameters.AddWithValue("@ownerEntityId", data.OwnerEntityId);
                request.Parameters.AddWithValue("@maxPlayers", data.MaxPlayers);
                request.Parameters.AddWithValue("@minRank", data.MinRank);
                request.Parameters.AddWithValue("@maxRank", data.MaxRank);
                request.Parameters.AddWithValue("@createdAt", data.CreatedAt);
                request.Parameters.AddWithValue("@parametersJson", data.ParametersJson);

                ServerMonitor.RegisterDbWrite();
                return await request.ExecuteNonQueryAsync() > 0;
            }
        }

        public async Task<bool> Remove(long battleId)
        {
            using (var request = new SQLiteCommand(
                "DELETE FROM battles WHERE battle_id = @battleId;",
                ServerDatabase.Connection))
            {
                request.Parameters.AddWithValue("@battleId", battleId);

                ServerMonitor.RegisterDbWrite();
                return await request.ExecuteNonQueryAsync() > 0;
            }
        }

        public async Task<int> RemoveByMapName(string mapName)
        {
            using (var request = new SQLiteCommand(
                "DELETE FROM battles WHERE lower(map_group) = lower(@mapName) OR lower(real_name) = lower(@mapName) OR lower(custom_name) = lower(@mapName);",
                ServerDatabase.Connection))
            {
                request.Parameters.AddWithValue("@mapName", mapName);

                ServerMonitor.RegisterDbWrite();
                return await request.ExecuteNonQueryAsync();
            }
        }

        public async Task<List<BattleDbRow>> List()
        {
            using (var request = new SQLiteCommand(
                "SELECT battle_id, custom_name, real_name, map_group, map_path, mode, owner_entity_id, max_players, min_rank, max_rank, created_at, parameters_json FROM battles ORDER BY created_at DESC;",
                ServerDatabase.Connection))
            {
                var result = new List<BattleDbRow>();
                ServerMonitor.RegisterDbRead();
                using (DbDataReader reader = await request.ExecuteReaderAsync(CommandBehavior.Default))
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(ReadRow(reader));
                    }
                }

                return result;
            }
        }

        private static BattleDbRow ReadRow(DbDataReader reader)
        {
            return new BattleDbRow
            {
                BattleId = reader.GetInt64(reader.GetOrdinal("battle_id")),
                CustomName = reader.GetString(reader.GetOrdinal("custom_name")),
                RealName = reader.GetString(reader.GetOrdinal("real_name")),
                MapGroup = reader.GetString(reader.GetOrdinal("map_group")),
                MapPath = reader.GetString(reader.GetOrdinal("map_path")),
                Mode = reader.GetString(reader.GetOrdinal("mode")),
                OwnerEntityId = reader.GetInt64(reader.GetOrdinal("owner_entity_id")),
                MaxPlayers = reader.GetInt32(reader.GetOrdinal("max_players")),
                MinRank = reader.GetInt32(reader.GetOrdinal("min_rank")),
                MaxRank = reader.GetInt32(reader.GetOrdinal("max_rank")),
                CreatedAt = reader.GetInt64(reader.GetOrdinal("created_at")),
                ParametersJson = reader.GetString(reader.GetOrdinal("parameters_json"))
            };
        }
    }

    public struct BattleDbRow
    {
        public long BattleId;
        public string CustomName;
        public string RealName;
        public string MapGroup;
        public string MapPath;
        public string Mode;
        public long OwnerEntityId;
        public int MaxPlayers;
        public int MinRank;
        public int MaxRank;
        public long CreatedAt;
        public string ParametersJson;
    }
}
