using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections;
using System.Collections.Generic;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif
using System.IO;
using System.Linq;

public class GoogleAnalyticsPostProcess : MonoBehaviour
{
    /**
     * Runs when Post-Export method has been set to
     * 'PostBuildProcessor.OnPostprocessBuildiOS' in your Unity Cloud Build
     * target settings.
     */
    #if UNITY_CLOUD_BUILD
    // This method is added in the Advanced Features Settings on UCB
    // PostBuildProcessor.OnPostprocessBuildiOS
    public static void OnPostprocessBuildiOS (string exportPath)
    {
        ProcessPostBuild(BuildTarget.iOS,exportPath);
    }
    #endif

    /**
     * Runs after successful build of an iOS-targetted Unity project
     * via the editor Build dialog.
     */
    [PostProcessBuild]
    public static void OnPostprocessBuild (BuildTarget buildTarget, string path)
    {
        #if !UNITY_CLOUD_BUILD
        ProcessPostBuild (buildTarget, path);
        #endif
    }

    /**
     * This ProcessPostBuild method will run via Unity Cloud Build, as well as
     * locally when build target is iOS. Using the Xcode Manipulation API, it is
     * possible to modify build settings values and also perform other actions
     * such as adding custom frameworks. Link below is the reference documentation
     * for the Xcode Manipulation API:
     *
     * http://docs.unity3d.com/ScriptReference/iOS.Xcode.PBXProject.html
     */
    private static void ProcessPostBuild (BuildTarget buildTarget, string path)
    {
        // Only perform these steps for iOS builds
        #if UNITY_IOS

        Debug.Log ("[UNITY_IOS] ProcessPostBuild - Adding Google Analytics frameworks.");

        // Go get pbxproj file
        string projPath = path + "/Unity-iPhone.xcodeproj/project.pbxproj";

        // PBXProject class represents a project build settings file,
        // here is how to read that in.
        PBXProject proj = new PBXProject ();
        proj.ReadFromFile (projPath);

        // This is the Xcode target in the generated project
        string target = proj.TargetGuidByName("Unity-iPhone");

        // List of frameworks that will be added to project
        List<string> frameworks = new List<string>() {
            "AdSupport.framework",
            "CoreData.framework",
            "SystemConfiguration.framework",
            "Security.framework"
        };

        // Add each by name
        frameworks.ForEach((framework) => {
            proj.AddFrameworkToProject(target, framework, false);
        });

        // If building with the non-bitcode version of the plugin, these lines should be uncommented.
        // Debug.Log("[UNITY_IOS] ProcessPostBuild - Setting build property: ENABLE_BITCODE = NO");
        // proj.AddBuildProperty(target, "ENABLE_BITCODE", "NO");

        // Write PBXProject object back to the file
        proj.WriteToFile (projPath);

        #endif
    }
}
