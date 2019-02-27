using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace FMODExtenstions
{
    /// <summary>
    /// Runtime manager for FMOD Extensions
    /// </summary>
    [AddComponentMenu("")]
    public class ExtensionsManager : MonoBehaviour
    {
        // References so there's less typing
        public FMOD.System LowLevelSystem { get { return RuntimeManager.LowlevelSystem; } }
        public FMOD.Studio.System StudioSystem { get { return RuntimeManager.StudioSystem; } }

        private static ExtensionsManager instance;
        public static ExtensionsManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("FMOD Extensions.Manager");
                    instance = go.AddComponent<ExtensionsManager>();
                    DontDestroyOnLoad(go);
                    go.hideFlags = HideFlags.HideInHierarchy;

                    instance.Initialize();

                    // Debug
                    StartCommandCapture();
                }
                return instance;
            }
        }

        void Initialize ()
        {
            instance.GetRecordingInformation();     // Find all recording drivers
            instance.ApplyExtensionSettings();      // Apply our new settings
            instance.SetupCallback();               // Callbacks for things like device changes

            UnityEngine.Debug.Log("FMOD Extensions: Creating manager");
        }

        void OnDestroy()
        {
            StopCommandCapture();
        }

        void CheckResult(RESULT result, string cause)
        {
            if (result != RESULT.OK)
                UnityEngine.Debug.LogError("FMOD Extensions: " + result + " during " + cause);
        }

        void ApplyExtensionSettings()
        {
            RESULT result = RESULT.OK;

            FMOD.ADVANCEDSETTINGS advancedSettings = new FMOD.ADVANCEDSETTINGS();

            result = RuntimeManager.LowlevelSystem.getAdvancedSettings(ref advancedSettings);
            CheckResult(result, "FMOD.GetAdvancedSettings");

            advancedSettings.geometryMaxFadeTime = Settings.Instance.GeometryMaxFadeTime;

            result = RuntimeManager.LowlevelSystem.setAdvancedSettings(ref advancedSettings);
            CheckResult(result, "FMOD.SetAdvancedSettings");
        }

        void SetupCallback()
        {
            LowLevelSystem.setCallback(SystemCallback, FMOD.SYSTEM_CALLBACK_TYPE.DEVICELISTCHANGED);
        }

        public RESULT SystemCallback(IntPtr systemraw, FMOD.SYSTEM_CALLBACK_TYPE type, IntPtr commanddata1, IntPtr commanddata2, IntPtr userdata)
        {
            RESULT result = RESULT.OK;
            switch (type)
            {
                case FMOD.SYSTEM_CALLBACK_TYPE.DEVICELISTCHANGED:
                    RefreshRecordingDrivers();
                    break;
            }
            return result;
        }

        class AttachedInstance
        {
            public Channel channel;
            public Transform transform;
            public Rigidbody rigidbody;
        }

        List<AttachedInstance> attachedInstances = new List<AttachedInstance>(128);

        void Update ()
        {
            // Get the listener position
            LowLevelSystem.get3DListenerAttributes(0, out pos, out vel, out forward, out up);
            listPos = ExtensionsUtils.ToUnityVector(pos);

            // Update the position of the low level sounds
            for (int i = 0; i < attachedInstances.Count; i++)
            {
                if (!attachedInstances[i].channel.hasHandle() || attachedInstances[i].transform == null)
                {
                    attachedInstances.RemoveAt(i); 
                    continue;
                }
                VECTOR instancePos = attachedInstances[i].transform.position.ToFMODVector();
                VECTOR zero = new VECTOR();
                VECTOR alt = new VECTOR();

                if (attachedInstances[i].rigidbody)
                {
                    VECTOR instanceVel = attachedInstances[i].rigidbody.velocity.ToFMODVector();
                    attachedInstances[i].channel.set3DAttributes(ref instancePos, ref instanceVel, ref alt);
                }
                attachedInstances[i].channel.set3DAttributes(ref instancePos, ref zero, ref alt);
            }

            // Check currently playing programmer sounds and remove them from the list if they stop playing or are invalid
            for (int i = 0; i < programmerInstances.Count; i++)
            {
                if (!programmerInstances[i].hasHandle() || !programmerInstances[i].isValid())
                {
                    UnityEngine.Debug.Log("Removing programmer sound from list"); 
                    programmerInstances.RemoveAt(i);
                }
            }
        }

        // ---------- Geometry ----------

        private VECTOR pos;
        VECTOR vel;
        VECTOR forward;
        VECTOR up;

        Vector3 listPos;

        public static FMOD.Geometry CreateGeometryObject (int maxPolygons, int maxVertices)
        {
            RESULT result = RESULT.OK;
            FMOD.Geometry g;

            result = Instance.LowLevelSystem.createGeometry(maxPolygons, maxVertices, out g);
            Instance.CheckResult(result, "FMOD.CreateGeometry");

            return g;
        }

        public static void AddPolygon (FMOD.Geometry geometry, Geometry.Polygon[] polygons)
        {
            RESULT result = RESULT.OK;

            for (int i = 0; i < polygons.Length; i++)
            {
                result = geometry.addPolygon(polygons[i].directOcclusion,
                                             polygons[i].reverbOcclusion,
                                             polygons[i].doubleSided,
                                             polygons[i].vertices.Length,
                                             ExtensionsUtils.ToFMODVectorArray(polygons[i].vertices),
                                             out polygons[i].polygonIndex);
                Instance.CheckResult(result, "FMOD.AddPolygon");
            }
        }

        /// <summary>
        /// Get direct occlusion amount from the emitter's position
        /// </summary>
        /// <returns>The occlusion amout.</returns>
        /// <param name="emitterPos">Emitter position.</param>
        public static float GetOcclusionAmout (Vector3 emitterPos)
        {
            RESULT result = RESULT.OK;
            float direct;
            float reverb;
            VECTOR emitterFPos = emitterPos.ToFMODVector();
            result = Instance.LowLevelSystem.getGeometryOcclusion(ref Instance.pos, ref emitterFPos, out direct, out reverb);
            return direct;
        }

        // ---------- Low Level Sounds ----------

        #region LowLevel Sounds

        public static RESULT LoadSound (string fileName, MODE mode, out Sound sound)
        {
            RESULT result = RESULT.OK;
            fileName = ExtensionsUtils.FindFile(fileName);
            Sound s;

            result = Instance.LowLevelSystem.createSound(fileName, mode, out s);
            Instance.CheckResult(result, "FMOD.CreateSound");


            sound = s;

            return result;
        }

        public static RESULT PlaySound(Sound sound, ChannelGroup channelGroup, out Channel channel, bool paused = false)
        {
            RESULT result = RESULT.OK;

            result = Instance.LowLevelSystem.playSound(sound, channelGroup, paused, out channel);
            Instance.CheckResult(result, "FMOD.PlaySound");

            return result;
        }

        public static void AttachToGameObject (Channel _channel, Transform _transform, Rigidbody _rigidbody)
        {
            var attachedInstance = new AttachedInstance();
            attachedInstance.channel = _channel;
            attachedInstance.transform = _transform;
            attachedInstance.rigidbody = _rigidbody;
            Instance.attachedInstances.Add(attachedInstance);
        }

        public static void DetachFromGameObject (Channel _channel)
        {
            for (int i = 0; i < Instance.attachedInstances.Count; i++)
            {
                if (Instance.attachedInstances[i].channel.handle == _channel.handle)
                {
                    Instance.attachedInstances.RemoveAt(i);
                    return;
                }
            }
        }

        /// <summary>
        /// Gets a low level channel group from a studio bus.
        /// If no bus is found then it will return the master channel group
        /// </summary>
        /// <returns>The channel group from bus.</returns>
        /// <param name="busPath">Bus.</param>
        public static ChannelGroup GetChannelGroupFromBus (string busPath)
        {
            RESULT result = RESULT.OK;
            Bus bus;
            ChannelGroup channelGroup;

            result = Instance.StudioSystem.getBus(busPath, out bus);

            if (result != RESULT.OK) {
                result = Instance.LowLevelSystem.getMasterChannelGroup(out channelGroup);
                Instance.CheckResult(result, "FMOD.GetMasterChannelGroup");
                return channelGroup;
            }

            result = bus.getChannelGroup(out channelGroup);  
            Instance.CheckResult(result, "Bus.GetChannelGroup");

            return channelGroup;
        }

        #endregion

        // ---------- Recording ----------

        private int NumberOfDrivers;
        private int NumberOfConnectedDrivers;

        private Dictionary<int, RecordDriver> allRecordDrivers = new Dictionary<int, RecordDriver>();
        private Dictionary<int, RecordingSound> recordingSounds = new Dictionary<int, RecordingSound>();

        /// <summary>
        /// Signature for <see cref="OnDriverRefresh"/>
        /// </summary>
        public delegate void DriverRefresh();
        /// <summary>
        /// Called when a driver is added or removed. Recording will stop so you must find the driver again or connect to another
        /// </summary>
        public event DriverRefresh OnDriverRefresh;

        #region Drivers

            #region Internal

            /// <summary>
            /// Called when Extensions is initalised. Will not update removed or added drivers
            /// </summary>
            void GetRecordingInformation ()
            {
                RESULT result = RESULT.OK;
                result = LowLevelSystem.getRecordNumDrivers(out NumberOfDrivers, out NumberOfConnectedDrivers);
                CheckResult(result, "FMOD.GetRecordNumDrivers");

                if (NumberOfDrivers == 0) {
                    UnityEngine.Debug.LogWarning("FMOD Extensions: No recording drivers found!");
                    return;
                }

                for (int i = 0; i < NumberOfDrivers; i++) {
                    RecordDriver tempDriver = new RecordDriver();
                    LowLevelSystem.getRecordDriverInfo(i, out tempDriver.name, Settings.Instance.DriverNameLength, out tempDriver.guid, out tempDriver.systemRate, out tempDriver.speakerMode, out tempDriver.speakerModeChannels, out tempDriver.state);
                    tempDriver.id = i;

                    if (!allRecordDrivers.ContainsKey(i))
                        allRecordDrivers.Add(i, tempDriver);
                }
            }

            // TODO: Check for more effective way of clearing removed drivers and adding new ones
            /// <summary>
            /// Called if the number of recording drivers is not equal to the number of drivers we have reference to.
            /// Will completely clear and refill our dictionary of drivers.
            /// Plus, it will stop all recording sounds.
            /// </summary>
            void RefreshRecordingDrivers ()
            {
                if (OnDriverRefresh != null)
                    OnDriverRefresh.Invoke();

                // Remove all our previous drivers as they're going to probably be in the wrong order or just not there anymore
                allRecordDrivers.Clear();

                // Get number of drivers
                RESULT result = RESULT.OK;
                result = LowLevelSystem.getRecordNumDrivers(out NumberOfDrivers, out NumberOfConnectedDrivers);
                CheckResult(result, "FMOD.GetRecordNumDrivers");

                // Add our new list of drivers
                for (int i = 0; i < NumberOfDrivers; i++)
                {
                    RecordDriver tempDriver = new RecordDriver();
                    LowLevelSystem.getRecordDriverInfo(i, out tempDriver.name, Settings.Instance.DriverNameLength, out tempDriver.guid, out tempDriver.systemRate, out tempDriver.speakerMode, out tempDriver.speakerModeChannels, out tempDriver.state);
                    tempDriver.id = i;
                    allRecordDrivers.Add(tempDriver.id, tempDriver);
                }

                // If there are sounds recording, stop them.
                // (Assumption) FMOD will stop recording anyway as connections will be lost
                if (recordingSounds.Count > 0)
                {
                    foreach (KeyValuePair<int, RecordingSound> entry in recordingSounds)
                    {
                        entry.Value.Stop();
                    }
                }
            }

            #endregion

        /// <summary>
        /// Prints all recording driver names to the console
        /// </summary>
        public static void DebugRecordingDriverNames()
        {
            #if UNITY_EDITOR
            foreach (KeyValuePair<int, RecordDriver> entry in Instance.allRecordDrivers)
            {
                UnityEngine.Debug.Log(entry.Value.name); 
            }
            #endif
        }

        /// <summary>
        /// Get a recording driver from its ID
        /// </summary>
        /// <returns>The recording driver info.</returns>
        /// <param name="id">Identifier.</param>
        public static RecordDriver GetRecordingDriverInfo (int id)
        {
            if (Instance.NumberOfDrivers == 0) {
                UnityEngine.Debug.LogError("FMOD Extensions: No recording drivers found. All drivers might have been unplugged or a new one has not been checked and seen");
                return null;
            }

            if (Instance.allRecordDrivers.ContainsKey(id))
                return Instance.allRecordDrivers[id];

            UnityEngine.Debug.LogError("FMOD Extensions: No recording driver found with that id! Returning null!");
            return null;

        }

        /// <summary>
        /// Get a recording driver from it guid. It is chepaer to use int ID, however
        /// </summary>
        /// <returns>The recording driver ingo.</returns>
        /// <param name="guid">GUID.</param>
        public static RecordDriver GetRecordingDriverInfo (Guid guid)
        {
            if (Instance.NumberOfDrivers == 0)
            {
                UnityEngine.Debug.LogError("FMOD Extensions: No recording drivers found. All drivers might have been unplugged or a new one has not been checked and seen");
                return null;
            }
            foreach (KeyValuePair<int, RecordDriver> entry in Instance.allRecordDrivers)
            {
                if (entry.Value.guid == guid)
                    return entry.Value;
                continue;
            }
            UnityEngine.Debug.LogError("FMOD Extensions: No recording driver found with that guid! Returning null!");
            return null;
        }

        /// <summary>
        /// Get a recording driver from its name. It is cheaper to use int ID, however
        /// </summary>
        /// <returns>The recording driver info.</returns>
        /// <param name="name">Name.</param>
        public static RecordDriver GetRecordingDriverInfo (string name)
        {
            if (Instance.NumberOfDrivers == 0)
            {
                UnityEngine.Debug.LogError("FMOD Extensions: No recording drivers found. All drivers might have been unplugged or a new one has not been checked and seen");
                return null;
            }
            UnityEngine.Debug.Log(Instance.allRecordDrivers.Count); 
            foreach (KeyValuePair<int, RecordDriver> entry in Instance.allRecordDrivers)
            {
                if (name == entry.Value.name)
                    return entry.Value;
                continue;
            }
            UnityEngine.Debug.LogError("FMOD Extensions: No recording driver found with that name! Returning null");
            return null;
        }

        /// <summary>
        /// Get all names of the recording drivers
        /// </summary>
        /// <returns>The all recording driver names.</returns>
        public static List<string> GetAllRecordingDriverNames ()
        {
            List<string> result = new List<string>();
            foreach (KeyValuePair<int, RecordDriver> entry in Instance.allRecordDrivers)
            {
                result.Add(entry.Value.name);
            }
            return result;
        }

        #endregion

        #region Recording

        /// <summary>
        /// Record what is coming in from the given driver
        /// </summary>
        /// <returns>The record.</returns>
        /// <param name="driver">Driver.</param>
        public static RecordingSound StartRecording (RecordDriver driver, ChannelGroup channelgroup = new ChannelGroup())
        {
            if (driver.id == -1)
                return new RecordingSound();

            RESULT result = RESULT.OK;

            CREATESOUNDEXINFO exInfo = new CREATESOUNDEXINFO();
            exInfo.cbsize = Marshal.SizeOf(exInfo);
            exInfo.numchannels = driver.speakerModeChannels;
            exInfo.format = SOUND_FORMAT.PCM16;
            exInfo.defaultfrequency = driver.systemRate;
            exInfo.length = (uint)driver.systemRate * (uint)driver.speakerModeChannels * 16;    // (FROM FMOD EXAMPLES): nativeRate * sizeof(short) * nativeChannels
                                                                                                // I'm just saying 16 as that's the size of a short
            Sound sound;
            result = Instance.LowLevelSystem.createSound(IntPtr.Zero, MODE.LOOP_NORMAL | MODE.OPENUSER, ref exInfo, out sound);
            Instance.CheckResult(result, "FMOD.CreateSound");

            result = Instance.LowLevelSystem.recordStart(driver.id, sound, true);
            Instance.CheckResult(result, "FMOD.RecordStart");

            uint soundLength = 0;
            result = sound.getLength(out soundLength, TIMEUNIT.PCM);
            Instance.CheckResult(result, "Sound.GetLength");

            // Add our currently recording sound to our dictionary so we can check its record position and do stuff to it in update
            RecordingSound recordingSound = new RecordingSound
            {
                driver = driver,
                sound = sound
            };
            if (channelgroup.hasHandle())
                recordingSound.channelGroup = channelgroup;

            RecordingSound temp = new RecordingSound();
            if (Instance.recordingSounds.TryGetValue(driver.id, out temp)) 
                return temp;
            
            Instance.recordingSounds.Add(driver.id, recordingSound);
            return recordingSound;
        }

        /// <summary>
        /// Stop recording on the given driver
        /// </summary>
        /// <param name="recordingSound">Driver.</param>
        public static void StopRecording (RecordingSound recordingSound)
        {
            if (recordingSound.channel.hasHandle())
            {
                RESULT result = RESULT.OK;

                // Stop and remove channel
                result = recordingSound.channel.stop();
                Instance.CheckResult(result, "Channel.Stop");
                recordingSound.channel.clearHandle();

                // Stop recording and remove sound
                result = Instance.LowLevelSystem.recordStop(recordingSound.driver.id);
                recordingSound.sound.clearHandle();

                // Reset recording pos
                recordingSound.recordPos = 0;

                // Remove this from dictionary so we don't update it
                Instance.recordingSounds.Remove(recordingSound.driver.id);
            }
            else
            {
                UnityEngine.Debug.Log("FMOD Extensions: Recording Channel is null. Have you begun playback?");
            }
        }

        /// <summary>
        /// Stop all recordings
        /// </summary>
        public static void StopAllRecordings ()
        {
            foreach (KeyValuePair<int, RecordingSound> entry in Instance.recordingSounds)
            {
                StopRecording(entry.Value);
            }
        }

        /// <summary>
        /// Starts to play the sound with the given ID.
        /// Called by RecordingSound struct
        /// </summary>
        /// <param name="id">Identifier.</param>
        public static void PlayRecording (RecordingSound recordingSound)
        {
            if (recordingSound.channel.hasHandle())
            {
                recordingSound.channel.setPaused(false);
            }
            else
            {
                if (recordingSound.sound.hasHandle())
                {
                    if (recordingSound.recordPos >= Settings.Instance.RecordingLatency)
                    {
                        RESULT result = RESULT.OK;

                        if (recordingSound.channelGroup.hasHandle())
                        {
                            result = Instance.LowLevelSystem.playSound(recordingSound.sound, recordingSound.channelGroup, true, out recordingSound.channel);
                            Instance.CheckResult(result, "FMOD.PlaySound");

                            recordingSound.channel.setReverbProperties(0, 0f);

                            recordingSound.channel.setVolume(0f);
                            recordingSound.channel.setPaused(false);

                            recordingSound.channel.setVolume(1f);

                            Instance.StartCoroutine(Instance.VolumeRamp(recordingSound, 1f));


                        }
                        else
                        {
                            result = Instance.LowLevelSystem.getMasterChannelGroup(out recordingSound.channelGroup);
                            Instance.CheckResult(result, "FMOD.GetMasterChannelGroup");

                            result = Instance.LowLevelSystem.playSound(recordingSound.sound, recordingSound.channelGroup, true, out recordingSound.channel);
                            Instance.CheckResult(result, "FMOD.PlaySound");

                            recordingSound.channel.setReverbProperties(0, 0f);

                            recordingSound.channel.setVolume(0f);
                            recordingSound.channel.setPaused(false);

                            recordingSound.channel.setVolume(1f);

                            Instance.StartCoroutine(Instance.VolumeRamp(recordingSound, 1f));
                        }
                    }
                    else
                    {
                        Instance.StartCoroutine(Instance.WaitForBuffer(recordingSound));
                    } 
                }
            }
        }

        #endregion

        private IEnumerator VolumeRamp (RecordingSound recordingSound, float volume)
        {
            float realVolume = 0f;
            while (realVolume < volume)
            {
                float nextVolume = realVolume + 0.1f;
                recordingSound.channel.setVolume(nextVolume);
                recordingSound.channel.getVolume(out realVolume);
                yield return null;
            }
            yield return null;
        }

        private IEnumerator WaitForBuffer (RecordingSound recordingSound)
        {
            RESULT result = RESULT.OK;
            while (recordingSound.recordPos < Settings.Instance.RecordingLatency) {
                
                bool recording = false;
                result = LowLevelSystem.isRecording(recordingSound.driver.id, out recording);

                if (!recording) {
                    recordingSound.Stop();
                    continue;
                }
                else {
                    result = LowLevelSystem.getRecordPosition(recordingSound.driver.id, out recordingSound.recordPos);
                    CheckResult(result, "FMOD.GetRecordPosition");
                }
                yield return null;
            }
            PlayRecording(recordingSound);
            yield return null;
        }

        // ---------- Studio Command Capture ----------

        static string RecordFileName = "fmodcommands.txt";

        string GetCommandFilePath ()
        {
            string fullPath = Application.dataPath + "/Plugins/FMOD/Low Level/" + RecordFileName;
            if (!File.Exists(fullPath))
            {
                UnityEngine.Debug.Log("Creating File");
                File.Create(fullPath);
            }
            return Application.dataPath + "/Plugins/FMOD/Low Level/" + RecordFileName;
        }

        public static void StartCommandCapture ()
        {
            RESULT result = RESULT.OK;
            result = Instance.StudioSystem.startCommandCapture(Instance.GetCommandFilePath(), COMMANDCAPTURE_FLAGS.NORMAL);
            Instance.CheckResult(result, "Studio.StartCommandCapture");
        }

        public static void LoadCommandCapture ()
        {
            CommandReplay replay;
            RESULT result = RESULT.OK;
            result = Instance.StudioSystem.loadCommandReplay(Instance.GetCommandFilePath(), COMMANDREPLAY_FLAGS.NORMAL, out replay);

            int commandCount;
            result = replay.getCommandCount(out commandCount);
        }

        public static void StopCommandCapture ()
        {
            //Instance.studioSystem.flushCommands();
            //Instance.studioSystem.stopCommandCapture();
        }

        // ---------- Programmer Sounds ----------

        class ProgrammerSoundContext
        {
            public string file;
            public MODE mode;
        }

        /// <summary>
        /// Play an audio file inside an FMOD Event. Will play once without taking in 3D position
        /// </summary>
        /// <param name="audioFile">Audio file.</param>
        /// <param name="eventName">Event name.</param>
        public static void PlayOneShotProgrammerSound (string audioFile, string eventName)
        {
            audioFile = ExtensionsUtils.FindFile(audioFile);

            // Create the callback that will handle creating and destroying the programmer sound
            EVENT_CALLBACK callback = new EVENT_CALLBACK(ProgrammerEventCallback);

            // Create our one time event
            EventDescription eventDescription = RuntimeManager.GetEventDescription(eventName);
            EventInstance eventInstance; 
            eventDescription.createInstance(out eventInstance);
                
            // Set callback
            eventInstance.setCallback(callback);

            // Create our user data that we'll user later when playing the event
            ProgrammerSoundContext context = new ProgrammerSoundContext
            {
                file = audioFile,
                mode = MODE.CREATESAMPLE | MODE.LOOP_NORMAL | MODE.NONBLOCKING
            };

            // Create our pointer. It is unpinned when we get this data later
            GCHandle handle = GCHandle.Alloc(context, GCHandleType.Pinned);
            eventInstance.setUserData(GCHandle.ToIntPtr(handle));

            eventInstance.start();
            eventInstance.release();
        }

        /// <summary>
        /// Play an audio file inside an FMOD event. Will play once at a given position
        /// </summary>
        /// <param name="audioFile">Audio file.</param>
        /// <param name="eventName">Event name.</param>
        /// <param name="position">Position.</param>
        public static void PlayOneShotProgrammerSound (string audioFile, string eventName, Vector3 position)
        {
            audioFile = ExtensionsUtils.FindFile(audioFile);

            EVENT_CALLBACK callback = new EVENT_CALLBACK(ProgrammerEventCallback);

            EventDescription eventDescription = RuntimeManager.GetEventDescription(eventName);
            EventInstance eventInstance;
            eventDescription.createInstance(out eventInstance);

            eventInstance.set3DAttributes(RuntimeUtils.To3DAttributes(position));

            eventInstance.setCallback(callback);

            ProgrammerSoundContext context = new ProgrammerSoundContext();
            context.file = audioFile;
            context.mode = MODE.CREATESAMPLE | MODE._3D_LINEARSQUAREROLLOFF | MODE.LOOP_NORMAL | MODE.NONBLOCKING;

            GCHandle handle = GCHandle.Alloc(context, GCHandleType.Pinned);
            eventInstance.setUserData(GCHandle.ToIntPtr(handle));

            eventInstance.start();
            eventInstance.release();
        }

        private static List<EventInstance> programmerInstances = new List<EventInstance>(64);

        public static void PlayProgrammerSound (string audioFile, EventInstance instance)
        {
            // If we've already played this event and given it user data,
            // we only need to change the user data
            if (programmerInstances.Contains(instance))
            {
                // Get the user data that should be attached to the object
                IntPtr pointer;
                instance.getUserData(out pointer);

                GCHandle handle = GCHandle.FromIntPtr(pointer);
                ProgrammerSoundContext context = handle.Target as ProgrammerSoundContext;

                if (context != null)
                  context.file = ExtensionsUtils.FindFile(audioFile);
                // If there was no user data added, we'll have to add it here
                else
                {
                    context = new ProgrammerSoundContext();
                    context.file = ExtensionsUtils.FindFile(audioFile);
                    context.mode = MODE.CREATESAMPLE | MODE.LOOP_NORMAL | MODE.NONBLOCKING;

                    handle = GCHandle.Alloc(context, GCHandleType.Pinned);
                    instance.setUserData(GCHandle.ToIntPtr(handle));

                    EVENT_CALLBACK callback = new EVENT_CALLBACK(ProgrammerEventCallback);
                    instance.setCallback(callback);
                }

                UnityEngine.Debug.Log("Changing User Data"); 
            }
            else
            {
                programmerInstances.Add(instance);

                ProgrammerSoundContext context = new ProgrammerSoundContext();
                context.file = ExtensionsUtils.FindFile(audioFile);
                context.mode = MODE.CREATESAMPLE | MODE.LOOP_NORMAL | MODE.NONBLOCKING;

                GCHandle handle = GCHandle.Alloc(context, GCHandleType.Pinned);
                instance.setUserData(GCHandle.ToIntPtr(handle));

                EVENT_CALLBACK callback = new EVENT_CALLBACK(ProgrammerEventCallback);
                instance.setCallback(callback);
            }
            instance.start();
        }

        // Handles creation and destructin of the programmer sound
        static RESULT ProgrammerEventCallback(EVENT_CALLBACK_TYPE type, EventInstance studioEvent, IntPtr parameterPtr)
        {
            EventInstance eventInstance = studioEvent;

            IntPtr pointer; 
            eventInstance.getUserData(out pointer);

            // This gets the user data we just gave to the event
            GCHandle handle = GCHandle.FromIntPtr(pointer);
            ProgrammerSoundContext context = handle.Target as ProgrammerSoundContext;

            switch (type)
            {
                case EVENT_CALLBACK_TYPE.CREATE_PROGRAMMER_SOUND:

                    UnityEngine.Debug.Log("FMOD Extensions: Creating Programmer Sound");
                    PROGRAMMER_SOUND_PROPERTIES props = (PROGRAMMER_SOUND_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(PROGRAMMER_SOUND_PROPERTIES));

                    RESULT result = RESULT.OK;

                    Sound sound;
                    result = Instance.LowLevelSystem.createSound(context.file, context.mode, out sound);
                    Instance.CheckResult(result, "FMOD.CreateSound");

                    if (result == RESULT.OK)
                    {
                        props.sound = sound.handle;
                        Marshal.StructureToPtr(props, parameterPtr, false);
                    }
                    break;

                case EVENT_CALLBACK_TYPE.DESTROY_PROGRAMMER_SOUND:
                    UnityEngine.Debug.Log("FMOD Extensions: Destroying Programmer Sound");
                    RESULT resultTwo = RESULT.OK;
                    PROGRAMMER_SOUND_PROPERTIES properties = (PROGRAMMER_SOUND_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(PROGRAMMER_SOUND_PROPERTIES));
                    Sound destroyingSound = new Sound { handle = properties.sound };

                    result = destroyingSound.release();
                    Instance.CheckResult(resultTwo, "Sound.Release");
                    break;

                case EVENT_CALLBACK_TYPE.DESTROYED:
                    UnityEngine.Debug.Log("FMOD Extensions: Destroying Event");
                    handle.Free();
                    break;
            }
            return RESULT.OK;
        }

        // ---------- Internet Streaming ----------

        /// <summary>
        /// Create a streaming sound. The sound will begin being loaded into memory but no channel will be created.
        /// </summary>
        /// <returns>The streaming sound.</returns>
        /// <param name="fileName">File name.</param>
        public static StreamingSound CreateStreamingSound (string fileName)
        {
            StreamingSound ss = new StreamingSound();
            ss.file = fileName;



            return new StreamingSound();
        }
    }

    public struct StreamingSound
    {
        public string file;
        public Sound sound;
        public Channel channel;
        public ChannelGroup channelGroup;


    }

    /// <summary>
    /// All info about a recording driver
    /// </summary>
    public class RecordDriver
    {
        public int id;
        public string name;
        public int nameLength;
        public Guid guid;
        public int systemRate;
        public SPEAKERMODE speakerMode;
        public int speakerModeChannels;
        public DRIVER_STATE state;

        public RecordDriver ()
        {
            id = -1;
            name = "";
            nameLength = 0;
            guid = Guid.Empty;
            systemRate = 0;
            speakerMode = SPEAKERMODE.DEFAULT;
            speakerModeChannels = 0;
            state = DRIVER_STATE.DEFAULT;
        }
    }

    /// <summary>
    /// A sound that is currently being recorded to
    /// </summary>
    public struct RecordingSound
    {
        public Sound sound;
        public ChannelGroup channelGroup;
        public RecordDriver driver;
        public uint recordPos;

        public Channel channel;

        /// <summary>
        /// Begin or resume playback
        /// </summary>
        public void Start()
        {
            ExtensionsManager.PlayRecording(this);
        }

        /// <summary>
        /// Pause the channel
        /// </summary>
        public void Pause()
        {
            if (channel.hasHandle())
            {
                channel.setPaused(true);
            }
            else
            {
                UnityEngine.Debug.LogWarning("FMOD Extensions: Recording Channel is null. Have you begun playback?");
            }
        }

        /// <summary>
        /// Stop playback, recording and remove sound from memory
        /// </summary>
        public void Stop()
        {
            ExtensionsManager.StopRecording(this);
        }
    }

}

