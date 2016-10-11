using Google.JarResolver;
using UnityEditor;

[InitializeOnLoad]
public static class DriveJarDependencies
{
	private static readonly string PluginName = "DriveApiAccess";
	public static PlayServicesSupport svcSupport;

	static DriveJarDependencies()
	{
        // this only handles google player services api stuff, not the raw google api jars... those have to be added manually.. ugh
		svcSupport = PlayServicesSupport.CreateInstance(PluginName, EditorPrefs.GetString("AndroidSdkRoot"), "ProjectSettings");
        svcSupport.DependOn("com.google.android.gms", "play-services-auth", "9.2.1");
        svcSupport.DependOn("com.android.support", "support-v13", "23.4.0");
	}
}
