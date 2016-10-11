// Copyright 2016 Google Inc.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

﻿using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class NativeShare : MonoBehaviour {
	public const int SHARE_IMAGE_REQUEST_CODE = 39486;
	public const int SHARE_LINK_REQUEST_CODE = 39487;

	private static readonly string DefaultObjectName = "NativeShare";
	private static readonly string CallbackMethodNameLink = "ShareCallbackLink";
	private static readonly string CallbackMethodNameImage = "ShareCallbackImage";

	public void ShareImage(string imagePath)
	{
		if (Application.platform == RuntimePlatform.Android)
		{

			AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
			AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent");

			intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
			AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri");
			AndroidJavaObject uriObject = uriClass.CallStatic<AndroidJavaObject>("parse", "file://" + imagePath);
			intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_STREAM"), uriObject);
			intentObject.Call<AndroidJavaObject>("setType", "image/jpeg");

			AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			AndroidJavaObject currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity");

			AndroidJavaObject jChooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intentObject, "");
			//currentActivity.Call("startActivity", jChooser);
			currentActivity.Call("startActivityForResult", jChooser, SHARE_IMAGE_REQUEST_CODE);
		}
		else if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			#if UNITY_IOS
			_NativeShare_ShareImage(imagePath, NativeShare.DefaultObjectName, NativeShare.CallbackMethodNameImage);
			#endif
		}
		else
		{
			Debug.LogWarning("Sharing image in the editor is ignored");
			StartCoroutine(EditorFakeShare(SHARE_IMAGE_REQUEST_CODE, 0));
		}
	}


	public void ShareCallbackImage(string noOp)
	{
		var viewMgr = FindObjectOfType<ViewManager>();
		viewMgr.OnActivityResult(SHARE_IMAGE_REQUEST_CODE, 0, "null");
	}

	public void ShareCallbackLink(string noOp)
	{
		var viewMgr = FindObjectOfType<ViewManager>();
		viewMgr.OnActivityResult(SHARE_LINK_REQUEST_CODE, 0, "null");
	}

	private IEnumerator EditorFakeShare(int requestCode, int resultCode)
	{
		// just delay and fake a return result
		yield return new WaitForSeconds(1.0f);

		// HACK: this is pretty lame, but it works for now...
		var viewMgr = FindObjectOfType<ViewManager>();
		viewMgr.OnActivityResult(requestCode, resultCode, "null");
	}


	public void ShareUrl(string url, string text, string popupText)
	{
		if (Application.platform == RuntimePlatform.Android)
		{
			using (AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent"))
			{
				using (AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent"))
				{
					intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
					intentObject.Call<AndroidJavaObject>("setType", "text/plain");
					if (!string.IsNullOrEmpty(text))
						intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_SUBJECT"), text);
					intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), url);
					// force a choice every-time
					using (AndroidJavaObject chooserIntent = intentClass.CallStatic<AndroidJavaObject>("createChooser", intentObject, popupText))
					{
						using (AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
						{
							using (AndroidJavaObject currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
							{
								//currentActivity.Call("startActivity", chooserIntent);
								currentActivity.Call("startActivityForResult", chooserIntent, SHARE_LINK_REQUEST_CODE);
							}
						}
					}
				}
			}
		}
		else if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			#if UNITY_IOS
			_NativeShare_ShareUrl(url, text, NativeShare.DefaultObjectName, NativeShare.CallbackMethodNameLink);
			#endif
		}
		else
		{
			Debug.LogWarning("Sharing url in the editor is ignored");
			StartCoroutine(EditorFakeShare(SHARE_LINK_REQUEST_CODE, 0));
		}
	}

#if UNITY_IOS
	[DllImport("__Internal")]
	private static extern void _NativeShare_ShareImage (string iosPath, string callbackObjectName, string methodName);
	[DllImport("__Internal")]
	private static extern void _NativeShare_ShareUrl (string url, string title, string callbackObjectName, string methodName);
#endif
}
