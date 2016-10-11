using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif
using System.IO;

public class EnableObjCExceptions_BuildPostProcess
{

    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget buildTarget, string path)
    {
#if UNITY_IOS
        if (buildTarget == BuildTarget.iOS)
        {
            string projPath = path + "/Unity-iPhone.xcodeproj/project.pbxproj";

            PBXProject proj = new PBXProject();
            proj.ReadFromString(File.ReadAllText(projPath));

            string target = proj.TargetGuidByName("Unity-iPhone");

            proj.SetBuildProperty(target, "GCC_ENABLE_OBJC_EXCEPTIONS", "true");

            File.WriteAllText(projPath, proj.WriteToString());
        }
#endif
    }
}
