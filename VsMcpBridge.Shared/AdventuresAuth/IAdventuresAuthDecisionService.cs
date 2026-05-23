namespace VsMcpBridge.Shared.AdventuresAuth
{
    public interface IAdventuresAuthDecisionService
    {
        bool UsesPersistence { get; }

        string StorageKind { get; }

        AdventuresAuthDecision Login(AdventuresAuthRequest request);

        AdventuresAuthDecision ValidateSession(AdventuresAuthRequest request);

        AdventuresAuthDecision Logout(AdventuresAuthRequest request);

        AdventuresAuthDecision CurrentPrincipal(AdventuresAuthRequest request);
    }
}
