using SQLite;

namespace MatchMemoApp.Models
{
    [Table("Players")]
    public class Player
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; }
        public string Position { get; set; }
        public int Number { get; set; }
        public string PreferredFoot { get; set; } // "右足", "左足", "両足"
        public int Height { get; set; } // cm
        public string ImagePath { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}