using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class TestBenchEditor
{
    [MenuItem("Testing/BuildServer")]
    public static void BuildTestingServer()
    {
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
        var buildPlayerOptions = new BuildPlayerOptions()
        {
            scenes = new string[] { "Assets/Tests/Server/TestServer.unity" },
            options = BuildOptions.EnableHeadlessMode,
#if UNITY_EDITOR_OSX
            target = BuildTarget.StandaloneOSX,
            locationPathName = "Build/MacOS/TestingServer.app",
#endif
        };
        
        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }
}
