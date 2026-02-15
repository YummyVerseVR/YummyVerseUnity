using System;
using R3;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model
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
            // Guidパースできなかったら失敗として返す
            if (!Guid.TryParse(value, out var guid)) return result;
            
            result.IsValid = true;
            result.Guid = guid;
            return result;
        }
    }
}