using System;
using R3;

namespace YummyVerse.Scripts.ViewModel.Interface
{
    /// <summary>
    /// Optional v2 credentials port consumed by the settings presenter.  Keeping it
    /// separate from the legacy endpoint view-model port lets older scene/test
    /// consumers continue to compile while a new token field is introduced.
    /// </summary>
    public interface IYummyServiceV2ConfigViewModel
    {
        ReactiveProperty<string> APIDeviceToken { get; }
        event Action OnAPIDeviceTokenValidationError;
        void UpdateDeviceAccessToken(string token);
    }
}
