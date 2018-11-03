using System.Runtime.InteropServices;
using FMOD;
using FMODUnity;
using UnityEngine;

namespace FMODExtenstions
{
    /// <summary>
    /// Sound info coming in from the internet
    /// </summary>
    public struct InternetStreamData
    {
        public uint pos;
        public uint percent;
        public bool isPlaying;
        public bool isPaused;
        public bool isStarving;
        public bool isDiskBusy;
        public string state;

        public InternetStreamData (uint _pos, uint _percent, bool _isPlaying, bool _isPaused, bool _isStarving, bool _isDiskBusy, string _state)
        {
            pos = _pos;
            percent = _percent;
            isPlaying = _isPlaying;
            isPaused = _isPaused;
            isStarving = _isStarving;
            isDiskBusy = _isDiskBusy;
            state = _state;
        }
    }

    /// <summary>
    /// Extension of FMOD's low level features
    /// </summary>
    public class Fmod : MonoBehaviour
    {
        private static Fmod instance;
        public static Fmod Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("FMOD Extensions.FMOD");
                    instance = go.AddComponent<Fmod>();
                    DontDestroyOnLoad(go);
                    go.hideFlags = HideFlags.HideInHierarchy;
                }
                return instance;
            }
        }

        /// <summary>
        /// Takes a url and plays the audio found there
        /// </summary>
        /// <returns>The stream from internet.</returns>
        /// <param name="url">URL.</param>
        /// <param name="soundReturn">Sound return.</param>
        public static void PlaySoundFromNet(string url, out Sound _sound, out Channel _channel, out InternetStreamData _streamData)
        {
            // Low level
            FMOD.System system;
            ChannelGroup channelGroup;
            Sound sound;
            Channel channel;

            // Result and network enum
            RESULT result = RESULT.OK;
            OPENSTATE openState = OPENSTATE.READY;

            // Declare
            system = RuntimeManager.LowlevelSystem;
            system.getMasterChannelGroup(out channelGroup);         // Sound will be put on master channel group


            // Increase stream buffer size to account for internet lag
            // FMOD's example uses TIMEUNIT.RAWBYTES (8 bytes) but this caused the buffer to fill and the file to stop playing
            // TIMUNIT.PCMFRACTION is 16 bytes and has meant the file keeps playing
            result = system.setStreamBufferSize(64 * 1024, TIMEUNIT.PCMFRACTION);

            // Allocate memory for new sound object
            CREATESOUNDEXINFO exInfo = new CREATESOUNDEXINFO();
            exInfo.cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO));
            exInfo.filebuffersize = 1024 * 16;                          // Increase the default chunk size to hanlde seeking inside large playlist files that me be over 2kb

            // Create sound and return the sound object. Non blocking meaning it is opened / prepared in the background. sound.getOpenState gets the info about its loading
            result = system.createSound(url, MODE.CREATESTREAM | MODE.NONBLOCKING, ref exInfo, out sound);

            // Return if creating sound failed
            if (result != RESULT.OK)
            {
                UnityEngine.Debug.LogError("FMOD Extensions: Failed to create sound from net. " + result);
                _sound = new Sound();
                _channel = new Channel();
                _streamData = new InternetStreamData();
                return;
            }

            _sound = sound;

            // Info data returning from the sound object
            InternetStreamData streamData = new InternetStreamData(0, 0, false, false, false, false, "Stopped");

            // Main function for getting the information from the sound object
            result = sound.getOpenState(out openState, out streamData.percent, out streamData.isStarving, out streamData.isDiskBusy);

            // Return error message if we can't get the state of the sound
            if (result != RESULT.OK)
            {
                UnityEngine.Debug.LogError("FMOD Extensions: Could not get open state of file. " + result);
                _sound = new Sound();
                _channel = new Channel();
                _streamData = new InternetStreamData();
                return;
            }

            // Wait for file to become ready / for it to play
            while (openState != OPENSTATE.READY)
            {
                result = sound.getOpenState(out openState, out streamData.percent, out streamData.isStarving, out streamData.isDiskBusy);
            }

            result = system.playSound(sound, channelGroup, false, out channel);

            _channel = channel;

            if (!channel.hasHandle())
            {
                result = sound.getOpenState(out openState, out streamData.percent, out streamData.isStarving, out streamData.isDiskBusy);

                channel.getPaused(out streamData.isPaused);

                channel.isPlaying(out streamData.isPlaying);

                channel.getPosition(out streamData.pos, TIMEUNIT.MS);

                channel.setVolume(1f);

                UnityEngine.Debug.LogFormat("{0} {1} {2}", streamData.isPaused, streamData.isPlaying, streamData.pos);
            }

            channel.getPaused(out streamData.isPaused);

            channel.isPlaying(out streamData.isPlaying);

            channel.getPosition(out streamData.pos, TIMEUNIT.MS);

            if (openState == OPENSTATE.BUFFERING)
            {
                streamData.state = "Buffering";
            }
            else if (openState == OPENSTATE.CONNECTING)
            {
                streamData.state = "Connecting...";
            }
            else if (streamData.isPaused)
            {
                streamData.state = "Paused";
            }
            else if (streamData.isPlaying)
            {
                streamData.state = "Playing";
            }

            _streamData = streamData;

            UnityEngine.Debug.Log("FMOD Extensions: Successfully created stream from net");
        }

        public static Vector3 RotateAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
        {
            Fmod test = Instance;
            return Quaternion.Euler(angles) * (point - pivot) + pivot;
        }
    }
}


