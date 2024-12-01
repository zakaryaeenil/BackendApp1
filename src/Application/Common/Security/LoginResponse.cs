namespace NejPortalBackend.Application.Common.Security;
public class LoginResponse
{
    public string? TokenType { get; init; }
    public string? AccessToken { get; init; }
    public int ExpiresIn { get; init; }
    public string? RefreshToken { get; init; }
    public string? Error { get; init; }
}