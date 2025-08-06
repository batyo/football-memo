using SQLite;

namespace MatchMemoApp.Models
{
    [Table("Memos")]
    public class Memo
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int MatchId { get; set; }
        public int PlayerId { get; set; }
        public string Content { get; set; }
        public int MatchMinute { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}