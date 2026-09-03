using System;

namespace YummyVerse.Scripts.Model.Struct
{
    /// <summary>
    /// JSON transport types for the Unity Device projection.  These types intentionally
    /// mirror only fields exposed by the v2 Device API; Admin artifact metadata and
    /// preview URLs must not be inferred here.
    /// </summary>
    [Serializable]
    public sealed class DeviceOrderListResponseDto
    {
        public CustomerOrderStatusDto[] items;
        public string next_cursor;
        public bool has_more;
    }

    [Serializable]
    public sealed class CustomerOrderStatusDto
    {
        public string order_id;
        public string food_name;
        public string state;
        public CustomerStageStatusDto analysis;
        public CustomerOutputStatusDto generated_image;
        public CustomerOutputStatusDto glb;
        public CustomerOutputStatusDto wav;
        public string created_at;
        public string updated_at;
    }

    [Serializable]
    public sealed class CustomerStageStatusDto
    {
        public string state;
    }

    [Serializable]
    public sealed class CustomerOutputStatusDto
    {
        public string state;
        public bool downloadable;
        public string artifact_id;
    }

    [Serializable]
    public sealed class HardwarePayloadDto
    {
        public string order_id;
        public string payload_revision_id;
        public int revision;
        public string hardware_status;
        public string device_type;
        public string analysis_revision_id;
        public string profile_revision_id;
        public SerializableNumberMap control_values;
        public SerializableStringMap units;
        public HardwareSafetyConstraintDto[] safety_constraints;
        public string payload_sha256;
    }

    [Serializable]
    public sealed class DevicePayloadNotReadyDto
    {
        public string order_id;
        public string hardware_status;
        public object control_values;
    }

    [Serializable]
    public sealed class HardwareSafetyConstraintDto
    {
        // The contract deliberately allows additional properties.  Unity's
        // JsonUtility ignores those properties, which is safe because fail-closed
        // handling never derives a control value from this diagnostic collection.
        public string key;
        public string value;
    }

    [Serializable]
    public sealed class SerializableNumberMap
    {
        // JsonUtility cannot deserialize arbitrary object maps.  The DTO is retained
        // as a transport placeholder; Hardware Payload application stays disabled
        // until a typed mapping implementation is supplied by the hardware adapter.
    }

    [Serializable]
    public sealed class SerializableStringMap
    {
    }

    [Serializable]
    public sealed class HardwarePayloadAckRequestDto
    {
        public string payload_revision_id;
        public bool success;
        public string error_code;
        public string applied_at;
    }

    [Serializable]
    public sealed class HardwarePayloadAckDto
    {
        public string ack_id;
        public string order_id;
        public string payload_revision_id;
        public bool success;
        public string error_code;
        public string applied_at;
        public string result;
    }
}
