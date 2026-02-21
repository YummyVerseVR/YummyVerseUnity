using System;
using YummyVerse.Scripts.Model.Interface;

namespace YummyVerse.Scripts.Model
{
    public class EndPointManager : IEndPointManager
    {
        public string baseEndPointUrl { get; private set; } = "https://yummy-control-server.upiscium.dev/";
        public bool UpdateEndPointUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            // URLとして有効かチェック
            var result = Uri.TryCreate(url, UriKind.Absolute, out var uriResult);
            
            if (!result) return false;
            baseEndPointUrl = uriResult.ToString();
            return true;
        }
    }
}