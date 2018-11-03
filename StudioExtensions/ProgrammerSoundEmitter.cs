﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

namespace FMODExtenstions.Studio
{
    public class ProgrammerSoundEmitter : MonoBehaviour
    {
        public String File = "";

        [EventRef]
        public String Event = "";
        public EmitterGameEvent PlayEvent = EmitterGameEvent.None;
        public EmitterGameEvent StopEvent = EmitterGameEvent.None;
        public String CollisionTag = "";
        public bool AllowFadeout = true;
        public bool TriggerOnce = false;
        public bool Preload = false;
        public ParamRef[] Params = new ParamRef[0];
        public bool OverrideAttenuation = false;
        public float OverrideMinDistance = -1.0f;
        public float OverrideMaxDistance = -1.0f;

        private FMOD.Studio.EventDescription eventDescription;
        public FMOD.Studio.EventDescription EventDescription { get { return eventDescription; } }

        private FMOD.Studio.EventInstance instance;
        public FMOD.Studio.EventInstance EventInstance { get { return instance; } }

        private bool hasTriggered = false;
        private bool isQuitting = false;

        void Start() 
        {
            RuntimeUtils.EnforceLibraryOrder();
            if (Preload)
            {
                Lookup();
                eventDescription.loadSampleData();
                RuntimeManager.StudioSystem.update();
                FMOD.Studio.LOADING_STATE loadingState;
                eventDescription.getSampleLoadingState(out loadingState);
                while(loadingState == FMOD.Studio.LOADING_STATE.LOADING)
                {
#if WINDOWS_UWP
                    System.Threading.Tasks.Task.Delay(1).Wait();
#else
                    System.Threading.Thread.Sleep(1);
#endif
                    eventDescription.getSampleLoadingState(out loadingState);
                }
            }
            HandleGameEvent(EmitterGameEvent.ObjectStart);
        }

        void OnApplicationQuit()
        {
            isQuitting = true;
        }

        void OnDestroy()
        {
            if (!isQuitting)
            {
                HandleGameEvent(EmitterGameEvent.ObjectDestroy);
                if (instance.isValid())
                {
                    RuntimeManager.DetachInstanceFromGameObject(instance);
                }

                if (Preload)
                {
                    eventDescription.unloadSampleData();
                }
            }
        }

        void OnEnable()
        {
            HandleGameEvent(EmitterGameEvent.ObjectEnable);
        }

        void OnDisable()
        {
            HandleGameEvent(EmitterGameEvent.ObjectDisable);
        }

        void OnTriggerEnter(Collider other)
        {
            if (String.IsNullOrEmpty(CollisionTag) || other.CompareTag(CollisionTag))
            {
                HandleGameEvent(EmitterGameEvent.TriggerEnter);
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (String.IsNullOrEmpty(CollisionTag) || other.CompareTag(CollisionTag))
            {
                HandleGameEvent(EmitterGameEvent.TriggerExit);
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (String.IsNullOrEmpty(CollisionTag) || other.CompareTag(CollisionTag))
            {
                HandleGameEvent(EmitterGameEvent.TriggerEnter2D);
            }
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (String.IsNullOrEmpty(CollisionTag) || other.CompareTag(CollisionTag))
            {
                HandleGameEvent(EmitterGameEvent.TriggerExit2D);
            }
        }

        void OnCollisionEnter()
        {
            HandleGameEvent(EmitterGameEvent.CollisionEnter);
        }

        void OnCollisionExit()
        {
            HandleGameEvent(EmitterGameEvent.CollisionExit);
        }

        void OnCollisionEnter2D()
        {
            HandleGameEvent(EmitterGameEvent.CollisionEnter2D);
        }

        void OnCollisionExit2D()
        {
            HandleGameEvent(EmitterGameEvent.CollisionExit2D);
        }

        void HandleGameEvent(EmitterGameEvent gameEvent)
        {
            if (PlayEvent == gameEvent)
            {
                Play(File);
            }
            if (StopEvent == gameEvent)
            {
                Stop();
            }
        }

        void Lookup()
        {
            eventDescription = RuntimeManager.GetEventDescription(Event);
        }

        public void Play(string audioFile)
        {
            if (String.IsNullOrEmpty(audioFile))
                return;

            if (TriggerOnce && hasTriggered)
                return;

            if (String.IsNullOrEmpty(Event))
                return;

            if (!eventDescription.isValid())
                Lookup();

            bool isOneshot = false;
            if (!Event.StartsWith("snapshot", StringComparison.CurrentCultureIgnoreCase))
                eventDescription.isSnapshot(out isOneshot);

            bool is3D = false;
            eventDescription.is3D(out is3D);

            if (!instance.isValid())
                instance.clearHandle();

            if (isOneshot && instance.isValid())
            {
                instance.release();
                instance.clearHandle();
            }

            if (!instance.isValid())
            {
                eventDescription.createInstance(out instance);

                if (is3D)
                {
                    var rigidBody = GetComponent<Rigidbody>();
                    var rigidBody2D = GetComponent<Rigidbody2D>();
                    var transform = GetComponent<Transform>();
                    if (rigidBody)
                    {
                        instance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject, rigidBody));
                        RuntimeManager.AttachInstanceToGameObject(instance, transform, rigidBody);
                    }
                    else
                    {
                        instance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject, rigidBody2D));
                        RuntimeManager.AttachInstanceToGameObject(instance, transform, rigidBody2D);
                    }
                }
            }

            foreach (var param in Params)
            {
                instance.setParameterValue(param.Name, param.Value);
            }

            if (is3D && OverrideAttenuation)
            {
                instance.setProperty(FMOD.Studio.EVENT_PROPERTY.MINIMUM_DISTANCE, OverrideMinDistance);
                instance.setProperty(FMOD.Studio.EVENT_PROPERTY.MAXIMUM_DISTANCE, OverrideMaxDistance);
            }
            ExtensionsManager.PlayProgrammerSound(audioFile, this.instance);

            hasTriggered = true;
        }

        /// <summary>
        /// Play an audio file on this event instance
        /// </summary>
        /// <param name="audioFile">Audio file.</param>
        public void PlayAudioFile (string audioFile)
        {
            if (AllowFadeout)
                StartCoroutine(WaitForStop(audioFile));
            else
                Play(audioFile);
            
        }

        IEnumerator WaitForStop (string audio)
        {
            FMOD.Studio.PLAYBACK_STATE state;
            instance.getPlaybackState(out state);
            instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            while (state != FMOD.Studio.PLAYBACK_STATE.STOPPED)
            {
                instance.getPlaybackState(out state);
                yield return null;
            }
            Play(audio);
            yield return null;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                PlayAudioFile("BTS - Score Test (Scene 2).mp3");
        }

        public void Stop()
        {
            if (instance.isValid())
            {
                instance.stop(AllowFadeout ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
                instance.release();
                instance.clearHandle();
            }
        }
        
        public void SetParameter(string name, float value)
        {
            if (instance.isValid())
            {
                instance.setParameterValue(name, value);
            }
        }
        
        public bool IsPlaying()
        {
            if (instance.isValid() && instance.isValid())
            {
                FMOD.Studio.PLAYBACK_STATE playbackState;
                instance.getPlaybackState(out playbackState);
                return (playbackState != FMOD.Studio.PLAYBACK_STATE.STOPPED);
            }
            return false;
        }        
    }
}

