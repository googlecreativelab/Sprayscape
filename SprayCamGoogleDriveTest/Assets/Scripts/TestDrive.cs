using UnityEngine;
using System.Collections;
using System.IO;

public class TestDrive : MonoBehaviour
{
	public void Upload()
	{
		Texture2D t = new Texture2D(50, 50, TextureFormat.ARGB32, false);
		byte[] bytes = t.EncodeToJPG();

		string path = Path.Combine(Application.persistentDataPath, "test.jpeg");
		using (var fs = new FileStream(path, FileMode.OpenOrCreate))
		{
			fs.Write(bytes, 0, bytes.Length);
			fs.Close();
		}

		AndroidJavaClass activityClass = new AndroidJavaClass("com.androidexperiments.sprayscape.unitydriveplugin.GoogleDriveUnityPlayerActivity");
		AndroidJavaObject activity = activityClass.GetStatic<AndroidJavaObject>("activityInstance");
		bool res = activity.Call<bool>("uploadFile", "New Folder", "test.jpeg", path, "DriveReceiver");
		Debug.Log("uploadFile(): " + res);

	}
}
