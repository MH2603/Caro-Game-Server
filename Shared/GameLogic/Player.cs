namespace Shared.GameLogic
{
    [Serializable]
    public class PlayerData
    {
        public int Id;

        public string UserName = "";
        public string Password = "";

        // Always UTC
        public DateTime CreatedDate;
        public DateTime LastLoginDate;
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
