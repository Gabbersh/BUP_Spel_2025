using UnityEngine;

public class FullScreenManager : MonoBehaviour
{
    void Awake()
    {
#if !UNITY_WEBGL   // Kör bara i native builds (Android/iOS/PC/Mac)
        // Sätt fullscreenläge (för säkerhets skull)
        Screen.fullScreen = true;

        // Lås orienteringen till liggande (för runtime)
        Screen.orientation = ScreenOrientation.LandscapeLeft;

        // (Valfritt) Förhindra att användaren roterar
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
#endif
    }
}
