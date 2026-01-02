using UnityEngine;

public class MobileOrientationManager : MonoBehaviour
{
    private void Awake()
    {
        // Force landscape orientation on mobile devices
        if (Application.isMobilePlatform)
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
        }
    }
}

