using Dapper.Contrib.Extensions;

namespace Shared.GameLogic
{
    [Serializable]
    [Table("Players")] // must match the DB table name exactly
    public class PlayerData
    {
        [Key]
        public int Id { get; set; }

        public string Username { get; set; } = "";
        public string Password { get; set; } = "";

        // Always UTC
        public DateTime CreatedDate { get; set; }
        public DateTime LastLoginDate { get; set; }

        public int WinMatch { get; set; }
        public int LostMatch { get; set; }
    }

    public enum EPlayerState
    {
        None,
        Offline,
        Online,
        FindMatch,
        InMatch
    }

    public class Player
    {
        private PlayerData _data;
        private EPlayerState _state;

        public int Id => _data.Id;
        public PlayerData Data { get { return _data; } }

        public EPlayerState State
        {
            get => _state;
        }

        public Player(PlayerData data)
        {
            _data = data;

            ChangeState(EPlayerState.Offline);
        }

        public void ChangeState(EPlayerState newState)
        {
            if (State == newState)
            {
                return;
            }

            _state = newState;
        }
    }
}
