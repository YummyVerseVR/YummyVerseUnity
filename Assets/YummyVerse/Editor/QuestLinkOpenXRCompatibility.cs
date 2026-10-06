using System;
using UnityEditor;

// Quest Link must use Meta's hand tracking, rather than an installed Leap layer.
[InitializeOnLoad]
public static class QuestLinkOpenXRCompatibility
{
    static QuestLinkOpenXRCompatibility()
    {
        Environment.SetEnvironmentVariable("DISABLE_XR_APILAYER_ULTRALEAP_HAND_TRACKING_1", "1", EnvironmentVariableTarget.Process);
    }
}