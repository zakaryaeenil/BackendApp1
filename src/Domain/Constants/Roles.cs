namespace NejPortalBackend.Domain.Constants;

public abstract class Roles
{
    public const string Administrator = nameof(Administrator);
    public const string Agent = nameof(Agent);
    public const string Client = nameof(Client);

    public const string AdminAndAgent = "Administrator,Agent";
    public const string AdminAndAgentAndClient = "Administrator,Agent,Client";
}