using UnityEngine;
using FMOD;
using FMOD.Studio;
using FMODUnity;

namespace FMODExtenstions.Studio
{
    [RequireComponent(typeof(StudioEventEmitter))]
    public class StudioOcclusion : MonoBehaviour
    {
        protected StudioEventEmitter emitter;
        protected ParameterInstance parameter;
        private bool hasParameter = false;

        [SerializeField]
        protected string occlusionParameter;

        void Start()
        {
            if (emitter == null)
                emitter = GetComponent<StudioEventEmitter>();

            RESULT result = RESULT.OK;
            result = emitter.EventInstance.getParameter(occlusionParameter, out parameter);
            if (result == RESULT.OK)
                hasParameter = true;
        }

        void Update()
        {
            if (hasParameter)
            {
                if (emitter.IsPlaying())
                {
                    parameter.setValue(ExtensionsManager.GetOcclusionAmout(transform.position));
                }
            }
        }

        void OnValidate()
        {
            emitter = GetComponent<StudioEventEmitter>();
        }
    }
}

