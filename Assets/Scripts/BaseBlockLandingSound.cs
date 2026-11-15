using UnityEngine;

public class BaseBlockLandingSound : MonoBehaviour
{
    public ProceduralAudio proceduralAudio;

    public void PlayLandingSound()
    {
        if (proceduralAudio != null)
        {
            proceduralAudio.PlayMenuLanding();
        }
    }
}