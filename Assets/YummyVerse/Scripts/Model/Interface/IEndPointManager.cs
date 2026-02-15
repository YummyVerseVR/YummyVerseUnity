namespace YummyVerse.Scripts.Model.Interface
{
    public interface IEndPointManager
    {
        string baseEndPointUrl { get; }
        bool UpdateEndPointUrl(string url);
    }
}