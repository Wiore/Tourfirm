using Users;

namespace Web.Components.Services
{
    public class UserSession
    {
        public UserData? CurrentUser { get; private set; }

        public bool IsLoggedIn => CurrentUser is not null;
        public bool IsEmployee => CurrentUser?.Role == 1;

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
