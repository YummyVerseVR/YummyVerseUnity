using UnityEngine;
using UnityEngine.SceneManagement;

public class ControllerController : MonoBehaviour
{

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.A))
        {
            if (SceneManager.GetActiveScene().name == "Title")
            {
                SceneManager.LoadScene("QRTrackMR");
            }
            else
            {
                SceneManager.LoadScene("Title");
            }
        }  
    }
}
