using System.Net;

namespace YummyVerse.Scripts.Model.Struct
{
    public struct FoodDownloadResult
    {
        public HttpStatusCode StatusCode;
        public bool success => StatusCode == HttpStatusCode.OK;
        public Food Food;
    }
}