using System;
using UnityEngine;
using FMOD;
using FMODUnity;

namespace FMODExtenstions
{
    /// <summary>
    /// An emitter that allows for audio to be played outside of the Studio API
    /// </summary>
    [AddComponentMenu("FMOD Studio/Low Level/Emitter")]
    public class LowLevelEmitter : MonoBehaviour
    {
        public String File = "";
        public String Bus = "";
        public EmitterGameEvent StartSound = EmitterGameEvent.None;
        public EmitterGameEvent StopSound = EmitterGameEvent.None;
        public String CollisionTag = "";
        public bool Preload = false;
        public float MinDistance = 3f;
        public float MaxDistance = 10f;
        public float Volume = 1f;
        public Positioning Position = Positioning._3D_LINEAR_SQUARE_ROLLOFF;
        public LoopMode LoopMode = LoopMode.OFF;
        public LoadType LoadType = LoadType.SAMPLE;
        public bool UseReverb = false;

        private Sound sound;
        public Sound Sound { get { return sound; }}

        private Channel channel;
        public Channel Channel { get { return channel; }}

        private ChannelGroup channelGroup;
        public ChannelGroup ChannelGroup { get { return channelGroup; }}

        #region Mono Methods

        void Awake()
        {
            if (Preload)
                Load();
        }

        private void Start()
        {
            CheckGameEvent(EmitterGameEvent.ObjectStart);
        }

        private void OnDestroy()
        {
            CheckGameEvent(EmitterGameEvent.ObjectDestroy);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (String.IsNullOrEmpty(CollisionTag) || other.CompareTag(CollisionTag))
                CheckGameEvent(EmitterGameEvent.TriggerEnter);
        }

        private void OnTriggerExit(Collider other)
        {
            if (String.IsNullOrEmpty(CollisionTag) || other.CompareTag(CollisionTag))
                CheckGameEvent(EmitterGameEvent.TriggerExit);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (String.IsNullOrEmpty(CollisionTag) || other.CompareTag(CollisionTag))
                CheckGameEvent(EmitterGameEvent.TriggerEnter2D);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (String.IsNullOrEmpty(CollisionTag) || other.CompareTag(CollisionTag))
                CheckGameEvent(EmitterGameEvent.TriggerExit2D);
        }

        private void OnCollisionEnter()
        {
            CheckGameEvent(EmitterGameEvent.CollisionEnter);
        }

        private void OnCollisionExit()
        {
            CheckGameEvent(EmitterGameEvent.CollisionExit);
        }

        private void OnCollisionEnter2D()
        {
            CheckGameEvent(EmitterGameEvent.CollisionEnter2D);
        }

        private void OnCollisionExit2D()
        {
            CheckGameEvent(EmitterGameEvent.CollisionExit2D);
        }

        private void OnEnable()
        {
            CheckGameEvent(EmitterGameEvent.ObjectEnable);
        }

        private void OnDisable()
        {
            CheckGameEvent(EmitterGameEvent.ObjectDisable);
        }

        #endregion

        void CheckGameEvent(EmitterGameEvent gameEvent)
        {
            if (gameEvent == StartSound)
                Play();

            if (gameEvent == StopSound)
                Stop();
        }

        void Load ()
        {
            // Path on the computer as that is what FMOD understands
            string fullFilePath = Application.dataPath + Settings.Instance.AssetLocation + File;

            // Get the combination of all the modes to pass into the createSound method
            MODE finalMode = ExtensionsUtils.CreateModeForLowLevelEmitter(LoopMode, Position, LoadType);

            // Get the channel group
            channelGroup = ExtensionsManager.GetChannelGroupFromBus(Bus);

            // Finally, create
            ExtensionsManager.Instance.LowLevelSystem.createSound(fullFilePath, finalMode, out sound);
        }

        void Play ()
        {
            if (!sound.hasHandle())
                Load();

            ExtensionsManager.PlaySound(sound, channelGroup, out channel, true);

            if (Position != Positioning._2D)
            {
                VECTOR pos = RuntimeUtils.ToFMODVector(transform.position);
                VECTOR zero = new VECTOR();
                channel.set3DAttributes(ref pos, ref zero, ref zero);
                ExtensionsManager.AttachToGameObject(channel, transform, GetComponent<Rigidbody>());
                channel.set3DMinMaxDistance(MinDistance, MaxDistance);
            }

            if (!UseReverb)
            {
                channel.setReverbProperties(0, 0f);
            }

            channel.setVolume(Volume);
            channel.setPaused(false);
        }

        void Stop ()
        {
            if (channel.hasHandle())
            {
                channel.stop();
                channel.clearHandle();
                sound.release();
                sound.clearHandle();
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, MinDistance);

            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, MaxDistance);
        }
    }
}


