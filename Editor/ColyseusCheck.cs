using UnityEditor;
using UnityEngine;

namespace Mabar.Multiplayer.Editor
{
#if !MABAR_COLYSEUS
    [InitializeOnLoad]
    static class ColyseusCheck
    {
        static ColyseusCheck()
        {
            Debug.LogError(
                "[Mabarin SDK] Colyseus Unity SDK belum terinstall — SDK tidak akan berfungsi.\n\n" +
                "Install dulu via Package Manager:\n" +
                "  1. Window → Package Manager\n" +
                "  2. + → Add package from git URL\n" +
                "  3. https://github.com/colyseus/colyseus-unity-sdk.git#upm\n\n" +
                "Setelah Colyseus terinstall, error ini akan hilang otomatis.");
        }
    }
#endif
}
