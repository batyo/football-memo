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
        public string Opponent { get; set; }
        public string Stadium { get; set; }
        public string Formation { get; set; }
        public bool IsRealTimeMode { get; set; }
        public int CurrentMinute { get; set; }
        public bool IsTimerPaused { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}