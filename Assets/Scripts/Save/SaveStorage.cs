using System;
using System.IO;
using UnityEngine;

namespace ResortEmpire.Save
{
    /// <summary>Reads and writes the single local save file as JSON.</summary>
    public static class SaveStorage
    {
        private const string SaveFileName = "save.json";

        /// <summary>Location of the save file on this device.</summary>
        public static string DefaultPath => Path.Combine(Application.persistentDataPath, SaveFileName);

        /// <summary>True when a save file exists at <paramref name="path"/>.</summary>
        /// <param name="path">Save file location.</param>
        /// <returns>Whether there is a game to continue.</returns>
        public static bool Exists(string path) => File.Exists(path);

        /// <summary>Writes <paramref name="data"/> to <paramref name="path"/>, replacing the old save.</summary>
        /// <param name="data">State to store.</param>
        /// <param name="path">Save file location.</param>
        public static void Write(SaveData data, string path)
        {
            string temporaryPath = path + ".tmp";
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(data, true));
            if (File.Exists(path)) File.Delete(path);
            File.Move(temporaryPath, path);
        }

        /// <summary>Reads the save at <paramref name="path"/>.</summary>
        /// <param name="path">Save file location.</param>
        /// <param name="data">The stored state, or null when there is no readable save.</param>
        /// <returns>True when a save was read.</returns>
        public static bool TryRead(string path, out SaveData data)
        {
            data = null;
            if (!File.Exists(path)) return false;

            try
            {
                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            }
            catch (Exception exception) when (exception is ArgumentException or IOException)
            {
                Debug.LogWarning($"Save file {path} could not be read: {exception.Message}");
            }

            return data != null;
        }

        /// <summary>Removes the save at <paramref name="path"/> so the next game starts fresh.</summary>
        /// <param name="path">Save file location.</param>
        public static void Delete(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
