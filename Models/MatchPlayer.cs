using SQLite;

namespace MatchMemoApp.Models
{
    [Table("MatchPlayers")]
    public class MatchPlayer
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int MatchId { get; set; }
        public int PlayerId { get; set; }
        public double FieldX { get; set; } // フィールド上のX座標
        public double FieldY { get; set; } // フィールド上のY座標
        public bool IsStarting { get; set; }

        // 新しいフィールド
        public bool IsHomeTeam { get; set; } = true; // ホームチーム=true, アウェイチーム=false
    }
}