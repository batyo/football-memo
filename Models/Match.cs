using SQLite;
using System.ComponentModel.DataAnnotations;

namespace MatchMemoApp.Models
{
    [Table("Matches")]
    public class Match
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public DateTime Date { get; set; }
        public string Weather { get; set; }
        public string Opponent { get; set; } // 後方互換性のために残す
        public string Stadium { get; set; }
        public string Formation { get; set; } // 後方互換性のために残す
        public bool IsRealTimeMode { get; set; }
        public int CurrentMinute { get; set; }
        public bool IsTimerPaused { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // 新しいフィールド
        public string HomeTeamName { get; set; } = "ホームチーム";
        public string AwayTeamName { get; set; } = "アウェイチーム";
        public string HomeTeamFormation { get; set; } = "4-4-2";
        public string AwayTeamFormation { get; set; } = "4-4-2";
    }
}