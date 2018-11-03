using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMOD;

namespace FMODExtenstions
{
    public enum ReverbPreset
    {
        Off,
        Generic,
        PaddedCell,
        Room,
        Bathroom,
        LivingRoom,
        StoneRoom,
        Auditorium,
        ConcertHall,
        Cave,
        Arena,
        Hangar,
        CerpettedHallway,
        Hallway,
        StoneCorridor,
        Alley,
        Forest,
        City,
        Mountains,
        Quarry,
        Plain,
        ParkingLot,
        SewerPipe,
        Underwater
    }

    public enum LoopMode
    {
        OFF,
        ON
    }

    public enum Positioning
    {
        _2D,
        _3D,
        _3D_LINEAR_ROLLOFF,
        _3D_LINEAR_SQUARE_ROLLOFF,
        _3D_INVERSE_ROLLOFF
    }

    public enum LoadType
    {
        SAMPLE,
        STREAM
    }

    /// <summary>
    /// Static extension class for FMOD Extensions
    /// </summary>
    public static class ExtensionsUtils
    {
        /// <summary>
        /// Moves a position in relation to its pivot point and the angle it should move to
        /// </summary>
        /// <returns>The around pivot.</returns>
        /// <param name="point">Point.</param>
        /// <param name="pivot">Pivot.</param>
        /// <param name="angles">Angles.</param>
        public static Vector3 RotateAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
        {
            return Quaternion.Euler(angles) * (point - pivot) + pivot;
        }

        /// <summary>
        /// FMOD Vector to Unity Vector
        /// </summary>
        /// <returns>The unity vector.</returns>
        /// <param name="pos">Position.</param>
        public static Vector3 ToUnityVector (this VECTOR pos)
        {
            Vector3 uPos = new Vector3(pos.x, pos.y, pos.z);
            return uPos;
        }

        /// <summary>
        /// Converts an array of Unity Vectors to an array of FMOD Vectors
        /// </summary>
        /// <returns>The FMODV ector array.</returns>
        public static VECTOR[] ToFMODVectorArray(Geometry.Vertex[] vertices)
        {
            VECTOR[] array = new VECTOR[vertices.Length];

            for (int i = 0; i < vertices.Length; i++)
            {
                array[i] = FMODUnity.RuntimeUtils.ToFMODVector(vertices[i].position);
            }
            return array;
        }

        /// <summary>
        /// Total number of all vertices in a geometry object
        /// </summary>
        /// <returns>The total vertices in polygons.</returns>
        /// <param name="polygons">Polygons.</param>
        public static int GetTotalVerticesInPolygons (Geometry.Polygon[] polygons)
        {
            int numOfVertices = 0;
            for (int i = 0; i < polygons.Length; i++)
            {
                numOfVertices += polygons[i].vertices.Length;
            }
            return numOfVertices;
        }

        /// <summary>
        /// Will determin whether the low sound the user is creating should be treated as 2D or 3D
        /// </summary>
        /// <returns><c>true</c>, if low level sound is3 d was checked, <c>false</c> otherwise.</returns>
        /// <param name="modes">Modes.</param>
        public static bool CheckLowLevelSoundIs3D(MODE modes)
        {
            if ((modes & MODE._3D) == MODE._3D)
                return true;

            if ((modes & MODE._3D_CUSTOMROLLOFF) == MODE._3D_CUSTOMROLLOFF)
                return true;

            if ((modes & MODE._3D_HEADRELATIVE) == MODE._3D_CUSTOMROLLOFF)
                return true;

            if ((modes & MODE._3D_IGNOREGEOMETRY) == MODE._3D_IGNOREGEOMETRY)
                return true;

            if ((modes & MODE._3D_CUSTOMROLLOFF) == MODE._3D_CUSTOMROLLOFF)
                return true;

            if ((modes & MODE._3D_CUSTOMROLLOFF) == MODE._3D_CUSTOMROLLOFF)
                return true;

            if ((modes & MODE._3D_CUSTOMROLLOFF) == MODE._3D_CUSTOMROLLOFF)
                return true;
            
            return false;
        }

        /// <summary>
        /// Creates one MODE enum from an array of seperate properties
        /// </summary>
        /// <returns>The from array.</returns>
        /// <param name="array">Array.</param>
        public static MODE CombineFromArray (MODE[] array)
        {
            MODE result = MODE.DEFAULT;
            for (int i = 0; i < array.Length; i++)
            {
                result |= array[i];
            }
            return result;
        }

        public static MODE CreateModeForLowLevelEmitter (LoopMode l, Positioning p, LoadType t)
        {
            MODE mode = MODE.DEFAULT;
            mode |= ModeFromLoop(l) | ModeFromPosition(p) | ModeFromLoadType(t);
            return mode;
        }

        public static MODE ModeFromLoop (LoopMode loop)
        {
            MODE mode = MODE.DEFAULT;
            switch (loop)
            {
                case LoopMode.OFF:
                    mode = MODE.LOOP_OFF;
                    break;
                case LoopMode.ON:
                    mode = MODE.LOOP_NORMAL;
                    break;
            }
            return mode;
        }

        public static MODE ModeFromPosition (Positioning pos)
        {
            MODE mode = MODE.DEFAULT;
            switch (pos)
            {
                case Positioning._2D:
                    mode = MODE._2D;
                    break;
                case Positioning._3D:
                    mode = MODE._3D;
                    break;
                case Positioning._3D_LINEAR_ROLLOFF:
                    mode = MODE._3D_LINEARROLLOFF;
                    break;
                case Positioning._3D_LINEAR_SQUARE_ROLLOFF:
                    mode = MODE._3D_LINEARSQUAREROLLOFF;
                    break;
                case Positioning._3D_INVERSE_ROLLOFF:
                    mode = MODE._3D_INVERSEROLLOFF;
                    break;
            }
            return mode;
        }

        public static MODE ModeFromLoadType (LoadType type)
        {
            MODE mode = MODE.DEFAULT;
            switch (type)
            {
                case LoadType.SAMPLE:
                    mode = MODE.CREATESAMPLE;
                    break;
                case LoadType.STREAM:
                    mode = MODE.CREATESTREAM;
                    break;
            }
            return mode;
        }

        /// <summary>
        /// Finds the file within Settings.AssetLocation and gets its full path
        /// </summary>
        /// <returns>The file.</returns>
        /// <param name="fileName">File name.</param>
        public static string FindFile (string fileName)
        {
            // In case we've already created a full path and called this method
            if (File.Exists(fileName))
                return fileName;

            // Check if the file points directly to a file ie (Audio Files/Doom.wav)
            string fullFileName = Application.dataPath + Settings.Instance.AssetLocation + fileName;

            if (File.Exists(fullFileName))
                return fullFileName;

            // If the file contains a path but we wern't able to find it then the path must be wrong
            if (fileName.Contains("/"))
                throw new ArgumentException("File contains a path but the file could not be found at that location");

            // If we've got here, the file can't contain a path, so we must search through the directories to find it
            string[] directories = Directory.GetDirectories(Application.dataPath + Settings.Instance.AssetLocation);
        
            try
            {
                for (int i = 0; i < directories.Length; i++)
                {
                    string[] files = Directory.GetFiles(directories[i]);
                    for (int y = 0; y < files.Length; y++)
                    {
                        if (files[y].Contains(fileName))
                            return files[y];
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }

            UnityEngine.Debug.LogError("FMOD Extensions: Could not find file!"); 
            return "";
        }

        /// <summary>
        /// Gets an FMOD reverb preset from the FMOD Extension's preset enum
        /// </summary>
        /// <returns>The reverb to FMODR everb.</returns>
        /// <param name="preset">Preset.</param>
        public static REVERB_PROPERTIES ExtensionReverbToFMODReverb (ReverbPreset preset)
        {
            REVERB_PROPERTIES properties = new REVERB_PROPERTIES(); 

            switch (preset)
            {
                case ReverbPreset.Alley:
                    properties = PRESET.ALLEY();
                    break;
                case ReverbPreset.Arena:
                    properties = PRESET.ARENA();
                    break;
                case ReverbPreset.Auditorium:
                    properties = PRESET.AUDITORIUM();
                    break;
                case ReverbPreset.Bathroom:
                    properties = PRESET.BATHROOM();
                    break;
                case ReverbPreset.Cave:
                    properties = PRESET.CAVE();
                    break;
                case ReverbPreset.CerpettedHallway:
                    properties = PRESET.CARPETTEDHALLWAY();
                    break;
                case ReverbPreset.City:
                    properties = PRESET.CITY();
                    break;
                case ReverbPreset.ConcertHall:
                    properties = PRESET.CONCERTHALL();
                    break;
                case ReverbPreset.Forest:
                    properties = PRESET.FOREST();
                    break;
                case ReverbPreset.Generic:
                    properties = PRESET.GENERIC();
                    break;
                case ReverbPreset.Hallway:
                    properties = PRESET.HALLWAY();
                    break;
                case ReverbPreset.Hangar:
                    properties = PRESET.HANGAR();
                    break;
                case ReverbPreset.LivingRoom:
                    properties = PRESET.LIVINGROOM();
                    break;
                case ReverbPreset.Mountains:
                    properties = PRESET.MOUNTAINS();
                    break;
                case ReverbPreset.Off:
                    properties = PRESET.OFF();
                    break;
                case ReverbPreset.PaddedCell:
                    properties = PRESET.PADDEDCELL();
                    break;
                case ReverbPreset.ParkingLot:
                    properties = PRESET.PARKINGLOT();
                    break;
                case ReverbPreset.Plain:
                    properties = PRESET.PLAIN();
                    break;
                case ReverbPreset.Quarry:
                    properties = PRESET.QUARRY();
                    break;
                case ReverbPreset.Room:
                    properties = PRESET.ROOM();
                    break;
                case ReverbPreset.SewerPipe:
                    properties = PRESET.SEWERPIPE();
                    break;
                case ReverbPreset.StoneCorridor:
                    properties = PRESET.STONECORRIDOR();
                    break;
                case ReverbPreset.StoneRoom:
                    properties = PRESET.STONEROOM();
                    break;
                case ReverbPreset.Underwater:
                    properties = PRESET.UNDERWATER();
                    break;
            }

            return properties;
        }
    }
}


