using UnityEngine;
using FMOD;
using FMODUnity;

namespace FMODExtenstions.Reverb
{
    /// <summary>
    /// 3D Reverb that allows sounds to morph between reverb zones as the listener moves around the world
    /// </summary>
    public class Reverb : MonoBehaviour
    {
        public float minDistance = 5f;
        public float maxDistance = 20f;
        public ReverbPreset preset;

        Reverb3D reverb;
        REVERB_PROPERTIES properties;

        void Start()
        {
            // Create
            RESULT result = RESULT.OK;
            result = RuntimeManager.LowlevelSystem.createReverb3D(out reverb);

            if (result != RESULT.OK)
            {
                UnityEngine.Debug.LogWarning("FMOD Extensions: Could not create 3D Reverb. " +result);
                return;
            }

            // Get the reverb's properties (preset and position)
            properties = ExtensionsUtils.ExtensionReverbToFMODReverb(preset);
            reverb.setProperties(ref properties);
            VECTOR pos = RuntimeUtils.ToFMODVector(transform.position);

            // Set position, min and max distances
            result = reverb.set3DAttributes(ref pos, minDistance, maxDistance);

            if (result != RESULT.OK)
            {
                UnityEngine.Debug.LogError("FMOD Extensions: Could not set 3D attributes to the 3D reverb " +result, this);
                return;
            }

            UnityEngine.Debug.Log("FMOD Extensions: Created 3D Reverb");
        }

        void OnDestroy()
        {
            
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Settings.Instance.ReverbColor;
            Gizmos.DrawWireSphere(transform.position, maxDistance);

            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, minDistance);
        }

    }

}


