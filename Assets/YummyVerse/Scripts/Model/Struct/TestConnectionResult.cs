using System.Net;

namespace YummyVerse.Scripts.Model.Struct
{
    public struct TestConnectionResult
    {
        public bool success;
        public HttpStatusCode StatusCode;
    }
}