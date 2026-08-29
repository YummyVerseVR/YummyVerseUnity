using System;
using System.Text;
using R3;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Infrastructure
{
    /// <summary>
    /// QRコードの中身をValidationするクラス
    /// </summary>
    public class QRValueValidator : IQRValueValidator
    {
        public QRValidationResult Validate(string value)
        {
            QRValidationResult result = new QRValidationResult()
            {
                IsValid = false
            };
            value = RemoveControlCharacters(value);
            // Guidパースできなかったら失敗として返す
            if (!Guid.TryParse(value, out var guid)) return result;
            
            result.IsValid = true;
            result.Guid = guid;
            return result;
        }

        /// <summary>
        /// MRUKはバイト列をUTF-8でデコードしているだけなので、制御文字が混ざっている可能性がある。
        /// この関数では制御文字を除去する処理を実装している
        /// </summary>
        /// <param name="value">制御文字を除去したいstring</param>
        /// <returns>制御文字を除去したstring</returns>
        private static string RemoveControlCharacters(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            var builder = new StringBuilder(value.Length);
            foreach (var c in value)
            {
                if (!char.IsControl(c))
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }
    }
}
