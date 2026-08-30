namespace YummyVerse.Scripts.Model.Interface
{
    /// <summary>Persists the runtime configuration used by the YummyService v2 client.</summary>
    public interface IYummyServiceV2ConfigStore
    {
        bool TryLoad(out string endpointUrl, out string deviceAccessToken);
        void Save(string endpointUrl, string deviceAccessToken);
    }
}
