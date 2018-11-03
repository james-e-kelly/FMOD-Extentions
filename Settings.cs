using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FMODExtenstions
{
    /// <summary>
    /// For making sure we are compatable with the base version of FMOD
    /// </summary>
    public class VERSION
    {
        /// <summary>
        /// Version of FMOD the extension tool was made alongside
        /// </summary>
        public const int number = 0x00011002;                                   // 1.10.02

        /// <summary>
        /// Version of the extension tools. Is not used for anything other than keeping track of progress and updates 
        /// </summary>
        public const string version = "1.0.0";
    }

    /// <summary>
    /// Settings for FMOD extensions
    /// </summary>
    #if UNITY_EDITOR
    [InitializeOnLoad]
    #endif
    public class Settings : ScriptableObject
    {
        private const string assetName = "FmodExtensionsSettings";

        private static Settings instance;
        public static Settings Instance
        {
            get
            {
                // Load settings into resources
                if (instance == null)
                    instance = Resources.Load(assetName) as Settings;
                if (instance == null)
                {
                    UnityEngine.Debug.Log("Fmod Extensions: Could not find settings. Creating default");
                    instance = CreateInstance<Settings>();
                    instance.name = assetName;

                    #if UNITY_EDITOR
                    if (!System.IO.Directory.Exists("Assets/Resources"))
                    {
                        AssetDatabase.CreateFolder("Assets", "Resources");
                    }
                    AssetDatabase.CreateAsset(instance, "Assets/Resources/" + assetName + ".asset");
                    #endif
                }

                // Check FMOD even exists
                FMODUnity.Settings fmodSettings = FMODUnity.Settings.Instance;
                if (fmodSettings == null)
                {
                    UnityEngine.Debug.LogError("FMOD Extensions: Can't find base FMOD settings. Has FMOD been imported into the game?");
                    return instance;
                }

                var baseVersion = (int)FMOD.VERSION.number;
                var extVersion = (int)VERSION.number;

                if (baseVersion > extVersion)
                    UnityEngine.Debug.LogWarning("FMOD Extensions: Base version of FMOD is greater than the extension's. Some errors may occur");

                return instance;
            }
        }

        #if UNITY_EDITOR
        [MenuItem("FMOD/Extension Settings")]
        public static void EditSettings()
        {
            Selection.activeObject = Instance;
            EditorApplication.ExecuteMenuItem("Window/Inspector");
        }
        #endif

        [Header("Debug")]
        public bool DisplayDebug = false;

        public Color ReverbColor = Color.red;

        public Color VerticesColor = Color.black;

        [Header("Extension Settings")]
        public uint GeometryMaxFadeTime = 1000;

        public string AssetLocation = "/StreamingAssets/";

        public int DriverNameLength = 128;

        public int RecordingLatency = 50;

        public FMOD.SOUND_FORMAT RecordingFormat = FMOD.SOUND_FORMAT.PCM16;
    }
}

