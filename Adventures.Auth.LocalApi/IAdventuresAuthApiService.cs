namespace Adventures.Auth.LocalApi;

public interface IAdventuresAuthApiService
{
    bool UsesPersistence { get; }

    string StorageKind { get; }

    AdventuresAuthApiResponse Login(AdventuresAuthApiRequest request);

    AdventuresAuthApiResponse Logout(AdventuresAuthApiRequest request);

    AdventuresAuthApiResponse CurrentPrincipal(AdventuresAuthApiRequest request);

    AdventuresAuthApiResponse Validate(AdventuresAuthApiRequest request);
}
