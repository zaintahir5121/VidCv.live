namespace AGONECompliance.Client.Services;

public sealed class AuthState
{
    public bool IsAuthenticated { get; private set; }
    public string UserName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string AvatarUrl { get; private set; } = string.Empty;
    public string DisplayName => string.IsNullOrWhiteSpace(UserName) ? "Guest" : UserName;
    public string Initials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(UserName))
            {
                return "U";
            }

            var parts = UserName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Take(2)
                .Select(x => char.ToUpperInvariant(x[0]));
            return string.Concat(parts);
        }
    }

    public event Action? Changed;

    public bool Login(string userName, string password)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        IsAuthenticated = true;
        UserName = userName.Trim();
        Email = userName.Trim();
        AvatarUrl = $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(UserName)}&background=4779F7&color=fff";
        Changed?.Invoke();
        return true;
    }

    public void Logout()
    {
        IsAuthenticated = false;
        UserName = string.Empty;
        Email = string.Empty;
        AvatarUrl = string.Empty;
        Changed?.Invoke();
    }
}
