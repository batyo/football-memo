using SQLite;
using MatchMemoApp.Models;

namespace MatchMemoApp.Data
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _database;

        public DatabaseService()
        {

        }

        async Task Init()
        {
            if (_database is not null)
                return;

            _database = new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);
            await _database.CreateTableAsync<Match>();
            await _database.CreateTableAsync<Player>();
            await _database.CreateTableAsync<MatchPlayer>();
            await _database.CreateTableAsync<Memo>();

            // データベース更新（既存テーブルに新しいカラムを追加）
            await UpdateDatabaseSchema();
        }

        private async Task UpdateDatabaseSchema()
        {
            try
            {
                // Matchテーブルに新しいカラムを追加
                await _database.ExecuteAsync("ALTER TABLE Matches ADD COLUMN HomeTeamName TEXT DEFAULT 'ホームチーム'");
                await _database.ExecuteAsync("ALTER TABLE Matches ADD COLUMN AwayTeamName TEXT DEFAULT 'アウェイチーム'");
                await _database.ExecuteAsync("ALTER TABLE Matches ADD COLUMN HomeTeamFormation TEXT DEFAULT '4-4-2'");
                await _database.ExecuteAsync("ALTER TABLE Matches ADD COLUMN AwayTeamFormation TEXT DEFAULT '4-4-2'");

                // MatchPlayerテーブルに新しいカラムを追加
                await _database.ExecuteAsync("ALTER TABLE MatchPlayers ADD COLUMN IsHomeTeam INTEGER DEFAULT 1");
            }
            catch
            {
                // カラムが既に存在する場合はエラーを無視
            }
        }

        // Match操作
        public async Task<List<Match>> GetMatchesAsync()
        {
            await Init();
            return await _database.Table<Match>().OrderByDescending(m => m.Date).ToListAsync();
        }

        public async Task<Match> GetMatchAsync(int id)
        {
            await Init();
            return await _database.Table<Match>().Where(m => m.Id == id).FirstOrDefaultAsync();
        }

        public async Task<int> SaveMatchAsync(Match match)
        {
            await Init();
            if (match.Id != 0)
            {
                match.UpdatedAt = DateTime.Now;
                return await _database.UpdateAsync(match);
            }
            else
            {
                match.CreatedAt = DateTime.Now;
                match.UpdatedAt = DateTime.Now;
                return await _database.InsertAsync(match);
            }
        }

        // Player操作
        public async Task<List<Player>> GetPlayersAsync()
        {
            await Init();
            return await _database.Table<Player>().ToListAsync();
        }

        public async Task<int> SavePlayerAsync(Player player)
        {
            await Init();
            if (player.Id != 0)
                return await _database.UpdateAsync(player);
            else
            {
                player.CreatedAt = DateTime.Now;
                return await _database.InsertAsync(player);
            }
        }

        public async Task<int> DeletePlayerAsync(int playerId)
        {
            await Init();
            return await _database.DeleteAsync<Player>(playerId);
        }

        // Memo操作
        public async Task<List<Memo>> GetMemosForMatchAsync(int matchId)
        {
            await Init();
            return await _database.Table<Memo>()
                .Where(m => m.MatchId == matchId)
                .OrderBy(m => m.MatchMinute)
                .ToListAsync();
        }

        public async Task<List<Memo>> GetMemosForPlayerAsync(int matchId, int playerId)
        {
            await Init();
            return await _database.Table<Memo>()
                .Where(m => m.MatchId == matchId && m.PlayerId == playerId)
                .OrderBy(m => m.MatchMinute)
                .ToListAsync();
        }

        public async Task<int> SaveMemoAsync(Memo memo)
        {
            await Init();
            if (memo.Id != 0)
            {
                memo.UpdatedAt = DateTime.Now;
                return await _database.UpdateAsync(memo);
            }
            else
            {
                memo.CreatedAt = DateTime.Now;
                memo.UpdatedAt = DateTime.Now;
                return await _database.InsertAsync(memo);
            }
        }

        // MatchPlayer操作
        public async Task<List<MatchPlayer>> GetMatchPlayersAsync(int matchId)
        {
            await Init();
            return await _database.Table<MatchPlayer>()
                .Where(mp => mp.MatchId == matchId)
                .ToListAsync();
        }

        public async Task<List<MatchPlayer>> GetHomeTeamPlayersAsync(int matchId)
        {
            await Init();
            return await _database.Table<MatchPlayer>()
                .Where(mp => mp.MatchId == matchId && mp.IsHomeTeam == true)
                .ToListAsync();
        }

        public async Task<List<MatchPlayer>> GetAwayTeamPlayersAsync(int matchId)
        {
            await Init();
            return await _database.Table<MatchPlayer>()
                .Where(mp => mp.MatchId == matchId && mp.IsHomeTeam == false)
                .ToListAsync();
        }

        public async Task<int> SaveMatchPlayerAsync(MatchPlayer matchPlayer)
        {
            await Init();
            if (matchPlayer.Id != 0)
                return await _database.UpdateAsync(matchPlayer);
            else
                return await _database.InsertAsync(matchPlayer);
        }
    }

    public static class Constants
    {
        public const string DatabaseFilename = "MatchMemoSQLite.db3";

        public const SQLiteOpenFlags Flags =
            SQLiteOpenFlags.ReadWrite |
            SQLiteOpenFlags.Create |
            SQLiteOpenFlags.SharedCache;

        public static string DatabasePath =>
            Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename);
    }
}