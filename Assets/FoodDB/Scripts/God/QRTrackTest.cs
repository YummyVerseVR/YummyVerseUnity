using Meta.XR.MRUtilityKit;
using UnityEngine;

public class QRTrackTest : MonoBehaviour
{
    [SerializeField] private GameObject testFood;
    public void Start()
    {
        Debug.Log("すた〜と！");
    }
    
    public void OnTrackableAdded(MRUKTrackable trackable)
    {
        Debug.Log($"Trackable of type {trackable.TrackableType} added.");
        Instantiate(testFood, trackable.transform);
    }

    public void OnTrackableRemoved(MRUKTrackable trackable)
    {
        Debug.Log($"Trackable removed: {trackable.name}");
        Destroy(trackable.gameObject);
    }
}
