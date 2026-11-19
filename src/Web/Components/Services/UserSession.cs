using Users;

namespace Web.Components.Services
{
    /// <summary>
    /// Scoped-сервис: хранит текущего пользователя в рамках Blazor-цепочки (circuit).
    /// </summary>
    public class UserSession
    {
        public UserData? CurrentUser { get; private set; }

        public bool IsLoggedIn => CurrentUser is not null;
        public bool IsEmployee => CurrentUser?.Role == 1;

        /// <summary> Уведомляет подписчиков об изменении состояния (для перерисовки). </summary>
        public event Action? OnChange;

        public void SetUser(UserData user)
        {
            CurrentUser = user;
            OnChange?.Invoke();
        }

        public void Logout()
        {
            CurrentUser = null;
            OnChange?.Invoke();
        }
    }
}
