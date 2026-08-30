namespace YummyVerse.Scripts.Model.Interface
{
    /// <summary>
    /// Credentials used by the YummyService v2 Unity Device API.
    ///
    /// A device token is deliberately kept outside <see cref="IEndPointManager"/>
    /// so existing endpoint consumers do not accidentally start treating a secret
    /// as part of a URL. Implementations must not provide a build-time default.
    /// </summary>
    public interface IYummyServiceV2Credentials
    {
        /// <summary>Opaque token issued for a device whose type is UNITY.</summary>
        string DeviceAccessToken { get; }

        /// <summary>
        /// Replaces the token held by this running device.  The token is accepted
        /// only when it is non-empty and contains no whitespace/control characters.
        /// </summary>
        bool UpdateDeviceAccessToken(string token);

        /// <summary>Removes the token from the current process and persistent cache.</summary>
        void ClearDeviceAccessToken();
    }
}
