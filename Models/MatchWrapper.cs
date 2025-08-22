using CommunityToolkit.Mvvm.ComponentModel;

namespace MatchMemoApp.Models
{
    public partial class MatchWrapper : ObservableObject
    {
        public Match Match { get; }

        [ObservableProperty]
        private bool isSelected = false;

        public MatchWrapper(Match match)
        {
            Match = match;
        }

        // Match のプロパティへの直接アクセス
        public int Id => Match.Id;
        public DateTime Date => Match.Date;
        public string Weather => Match.Weather;
        public string Opponent => Match.Opponent;
        public string Stadium => Match.Stadium;
        public string Formation => Match.Formation;
        public bool IsRealTimeMode => Match.IsRealTimeMode;
        public int CurrentMinute => Match.CurrentMinute;
        public bool IsTimerPaused => Match.IsTimerPaused;
        public DateTime CreatedAt => Match.CreatedAt;
        public DateTime UpdatedAt => Match.UpdatedAt;
        public string HomeTeamName => Match.HomeTeamName;
        public string AwayTeamName => Match.AwayTeamName;
        public string HomeTeamFormation => Match.HomeTeamFormation;
        public string AwayTeamFormation => Match.AwayTeamFormation;
    }
}