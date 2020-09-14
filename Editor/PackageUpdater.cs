using UnityEditor;
using UnityEditor.PackageManager;

namespace GameWorkstore.NetworkLibrary
{
    public class PackageUpdater
    {
        [MenuItem("Help/PackageUpdate/NetworkLibrary")]
        public static void TrackPackages()
        {
            Client.Add("git://github.com/GameWorkstore/networklibrary.git");
        }
    }
}