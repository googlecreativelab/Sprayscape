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
using System.Collections.Generic;
using System.Runtime.InteropServices;

public interface IPermissionCallbackReceiver
{
	bool HasPermission(string permission);
	bool ShouldShowRequestPermissionRationale (string permission);
	void RequestPermission(string permission);
	void ShowDialog(string title, string message);
	void EnsureRequiredPermission(string permission, string requiredTitle, string requiredText, string permissionInstructionsAfterNeverAskAgainOrHomeOut, System.Action grantedCallback);

	void DialogCallback(string noOp);
	void PermissionCallback(string res);
}

internal class PermissionCallbackReceiverAndroid : MonoBehaviour, IPermissionCallbackReceiver
{
	private static readonly string DefaultObjectName = "PermissionCallbackReceiver";
	private static readonly string CallbackMethodName = "PermissionCallback";
	private static readonly string DialogCallbackMethodName = "DialogCallback";

	public static Dictionary<string, bool> KnownPermissions = new Dictionary<string, bool>();

	private AndroidJavaClass permissionRequesterClass = null;

	public PermissionCallbackReceiverAndroid()
	{
		if (Application.platform == RuntimePlatform.Android)
			permissionRequesterClass = new AndroidJavaClass("com.androidexperiments.sprayscape.androidutils.PermissionRequester");
	}

	public bool HasPermission(string permission)
	{
		if (Application.platform != RuntimePlatform.Android)
			return false;

		return permissionRequesterClass.CallStatic<bool>("hasPermission", permission);
	}

	public bool ShouldShowRequestPermissionRationale(string permission)
	{
		if (Application.platform != RuntimePlatform.Android)
			return false;

		return permissionRequesterClass.CallStatic<bool>("shouldShowRequestPermissionRationale", permission);
	}

	public void RequestPermission(string permission)
	{
		if (Application.platform != RuntimePlatform.Android)
			return;

		permissionRequesterClass.CallStatic("requestPermission", permission, DefaultObjectName, CallbackMethodName);
	}

	public void ShowDialog(string title, string message)
	{
		if (Application.platform != RuntimePlatform.Android)
			return;

		permissionRequesterClass.CallStatic("showDialog", title, message, DefaultObjectName, DialogCallbackMethodName);
	}

	public void ShowPermissionRationaleDialog(string permission, string title, string message)
	{
		if (Application.platform != RuntimePlatform.Android)
			return;

		permissionRequesterClass.CallStatic("showPermissionRationaleDialog", permission, title, message, DefaultObjectName, CallbackMethodName);
	}

	public void EnsureRequiredPermission(string permission, string requiredTitle, string requiredText, string permissionInstructionsAfterNeverAskAgainOrHomeOut, System.Action grantedCallback)
	{
		if (Application.platform != RuntimePlatform.Android)
			return;

		// NOTE: don't check manually here first, we want to ensure the callback gets call not matter what

		// we don't currently have the permission, so we need to request it over and over again till they accept!
		// we could spawn a new and unique permission object receiver for each request like this to be safe...but we won't do that here..

		System.Action<string, bool> callback = null;
		callback = (string perm, bool granted) =>
		{
			if (perm != permission)
				return;

			if (granted)
			{
				// all done
				grantedCallback();
				PermissionCallbackReceiver.PermissionRequestStatus -= callback;
				return;
			}

			// else the user denied the privilege
			// lets pop-up a dialog and explain this is a required permission and re-request
			if (ShouldShowRequestPermissionRationale(perm))
				ShowPermissionRationaleDialog(perm, requiredTitle, requiredText);
			else
				ShowPermissionRationaleDialog(perm, requiredTitle, requiredText + " " + permissionInstructionsAfterNeverAskAgainOrHomeOut);
		};

		PermissionCallbackReceiver.PermissionRequestStatus += callback;

		// make the first permission request
		RequestPermission(permission);
	}

	public void DialogCallback(string noOp)
	{
		if (Debug.isDebugBuild)
			Debug.Log("DialogCallback(noOp) called from Java/Android");

		PermissionCallbackReceiver.DispatchDialogClosed();
	}

	public void PermissionCallback(string res)
	{
		if (Debug.isDebugBuild)
			Debug.Log("PermissionCallback('" + res + "') called from Java/Android");

		string[] parts = res.Split(',');
		string perm = parts[0];
		bool granted = false;
		bool.TryParse(parts[1], out granted);

		KnownPermissions[perm] = granted;

		PermissionCallbackReceiver.DispatchPermissionRequestStatus (perm, granted);
	}
}

internal class PermissionCallbackReceiveriOS : MonoBehaviour, IPermissionCallbackReceiver
{
	private static readonly string DefaultObjectName = "PermissionCallbackReceiver";
	private static readonly string CallbackMethodName = "PermissionCallback";
	private static readonly string DialogCallbackMethodName = "DialogCallback";

	public static Dictionary<string, bool> KnownPermissions = new Dictionary<string, bool>();

	public PermissionCallbackReceiveriOS() 
	{
	}

	public bool HasPermission(string permission)
	{
		if (Application.platform != RuntimePlatform.IPhonePlayer)
			return false;

		if (permission == PermissionCallbackReceiver.CAMERA_PERMISSION) {
			bool hasPermission = _PermissionCallbackReceiver_HasCameraPermission ();
			if (Debug.isDebugBuild)
				Debug.Log("HasPermission() returned: " + hasPermission);
			return hasPermission;
		}
		return true;
	}

	public bool ShouldShowRequestPermissionRationale(string permission)
	{
		if (Application.platform != RuntimePlatform.IPhonePlayer)
			return false;

		return false;
	}

	public void RequestPermission(string permission)
	{
		if (Application.platform != RuntimePlatform.IPhonePlayer)
			return;

		return;
	}

	public void ShowDialog(string title, string message)
	{
		if (Application.platform != RuntimePlatform.IPhonePlayer)
			return;

		return;
	}

	public void ShowPermissionRationaleDialog(string permission, string title, string message)
	{
		if (Application.platform != RuntimePlatform.IPhonePlayer)
			return;

		if (Debug.isDebugBuild)
			Debug.Log("PermissionCallbackReceiver.ShowPermissionRationaleDialog()");

		if (permission == PermissionCallbackReceiver.CAMERA_PERMISSION) {
			_PermissionCallbackReceiver_RequestCamera(title, message, PermissionCallbackReceiveriOS.DefaultObjectName, PermissionCallbackReceiveriOS.CallbackMethodName);
		}

		return;
	}

	public void EnsureRequiredPermission(string permission, string requiredTitle, string requiredText, string permissionInstructionsAfterNeverAskAgainOrHomeOut, System.Action grantedCallback)
	{
		if (Application.platform != RuntimePlatform.IPhonePlayer)
			return;
		
		if (Debug.isDebugBuild)
			Debug.Log("PermissionCallbackReceiver.EnsureRequiredPermission()");
		
		if (HasPermission (permission)) {

			if (Debug.isDebugBuild)
				Debug.Log("HasPermission(permission) already has permission");
			grantedCallback ();
			return;
		}

		if (permission == PermissionCallbackReceiver.CAMERA_PERMISSION) {
			System.Action<string, bool> callback = null;
			callback = (string perm, bool granted) =>
			{
				if (perm != permission)
					return;

				if (granted)
				{
					// all done
					grantedCallback();
					PermissionCallbackReceiver.PermissionRequestStatus -= callback;
					return;
				}
				
				if (_PermissionCallbackReceiver_IsCameraDenied ()) {
					ShowPermissionRationaleDialog (permission, requiredTitle, requiredText + " " + permissionInstructionsAfterNeverAskAgainOrHomeOut);
				} else {
					ShowPermissionRationaleDialog (permission, requiredTitle, requiredText);
				}
			};

			PermissionCallbackReceiver.PermissionRequestStatus += callback;

			if (_PermissionCallbackReceiver_IsCameraDenied ()) {
				ShowPermissionRationaleDialog (permission, requiredTitle, requiredText + " " + permissionInstructionsAfterNeverAskAgainOrHomeOut);
			} else {
				ShowPermissionRationaleDialog (permission, requiredTitle, requiredText);
			}

		}

		return;
	}

	public void DialogCallback(string noOp)
	{
		if (Debug.isDebugBuild)
			Debug.Log("DialogCallback(noOp) called from iOS");

		PermissionCallbackReceiver.DispatchDialogClosed();
	}

	public void PermissionCallback(string res)
	{
		if (Debug.isDebugBuild)
			Debug.Log("PermissionCallback('" + res + "') called from iOS");

		string[] parts = res.Split(',');
		string perm = parts[0];
		bool granted = false;
		bool.TryParse(parts[1], out granted);

		KnownPermissions[perm] = granted;

		PermissionCallbackReceiver.DispatchPermissionRequestStatus (perm, granted);
	}
	
	[DllImport("__Internal")]
	private static extern void _PermissionCallbackReceiver_RequestCamera (string requireTitlte, string requireText, string objectName, string callbackName);
	[DllImport("__Internal")]
	private static extern bool _PermissionCallbackReceiver_HasCameraPermission ();
	[DllImport("__Internal")]
	private static extern bool _PermissionCallbackReceiver_IsCameraDenied ();
	//[DllImport("__Internal")]
	//private static extern void _PermissionCallbackReceiver_ShowDialog (string title, string message);
}

public class PermissionCallbackReceiver : MonoBehaviour, IPermissionCallbackReceiver
{
	private static readonly string DefaultObjectName = "PermissionCallbackReceiver";

    public static Dictionary<string, bool> KnownPermissions = new Dictionary<string, bool>();
	public static event System.Action<string, bool> PermissionRequestStatus;
    public static event System.Action DialogClosed;

	#if UNITY_ANDROID
	public static readonly string STORAGE_PERMISSION = "android.permission.WRITE_EXTERNAL_STORAGE";
	public static readonly string CAMERA_PERMISSION = "android.permission.CAMERA";
	public static readonly string CONTACTS_PERMISSION = "android.permission.GET_ACCOUNTS";
	#elif UNITY_IOS
	public static readonly string STORAGE_PERMISSION = "WRITE_EXTERNAL_STORAGE";
	public static readonly string CAMERA_PERMISSION = "CAMERA";
	public static readonly string CONTACTS_PERMISSION = "GET_ACCOUNTS";
	#else
	public static readonly string STORAGE_PERMISSION = "android.permission.WRITE_EXTERNAL_STORAGE";
	public static readonly string CAMERA_PERMISSION = "android.permission.CAMERA";
	public static readonly string CONTACTS_PERMISSION = "android.permission.GET_ACCOUNTS";
	#endif

	private IPermissionCallbackReceiver receiverImpl = null;

    PermissionCallbackReceiver()
    {
		if (Application.platform == RuntimePlatform.Android)
			receiverImpl = new PermissionCallbackReceiverAndroid ();
		else if(Application.platform == RuntimePlatform.IPhonePlayer)
			receiverImpl = new PermissionCallbackReceiveriOS ();
    }

    public bool HasPermission(string permission)
    {
		return receiverImpl.HasPermission (permission);
    }

    public bool ShouldShowRequestPermissionRationale(string permission)
    {
        if (Application.platform != RuntimePlatform.Android)
            return false;

		return receiverImpl.ShouldShowRequestPermissionRationale (permission);
    }

    public void RequestPermission(string permission)
    {
        if (Application.platform != RuntimePlatform.Android)
            return;
		
		receiverImpl.RequestPermission (permission);
    }

    public void ShowDialog(string title, string message)
    {
        if (Application.platform != RuntimePlatform.Android)
            return;
		
		receiverImpl.ShowDialog (title, message);
    }

    public void EnsureRequiredPermission(string permission, string requiredTitle, string requiredText, string permissionInstructionsAfterNeverAskAgainOrHomeOut, System.Action grantedCallback)
    {
		receiverImpl.EnsureRequiredPermission (permission, requiredTitle, requiredText, permissionInstructionsAfterNeverAskAgainOrHomeOut, grantedCallback);
    }

    public static PermissionCallbackReceiver GetPermissionCallbackReceiver()
    {
        var receiver = Object.FindObjectOfType<PermissionCallbackReceiver>();
        if (receiver == null)
        {
			var obj = new GameObject(DefaultObjectName, typeof(PermissionCallbackReceiver));
            receiver = obj.GetComponent<PermissionCallbackReceiver>();
        }

        return receiver;
    }

	public static void DispatchPermissionRequestStatus(string perm, bool granted) 
	{
		if (PermissionCallbackReceiver.PermissionRequestStatus != null)
		{
			PermissionCallbackReceiver.PermissionRequestStatus(perm, granted);
		}
	}

	public static void DispatchDialogClosed() 
	{
		if (PermissionCallbackReceiver.DialogClosed != null)
		{
			PermissionCallbackReceiver.DialogClosed();
		}
	}

	public void DialogCallback(string noOp)
	{
		if (Debug.isDebugBuild)
			Debug.Log("DialogCallback(noOp) called from native");
		receiverImpl.DialogCallback (noOp);
	}

	public void PermissionCallback(string res)
	{
		if (Debug.isDebugBuild)
			Debug.Log("PermissionCallback('" + res + "') called from native");


		receiverImpl.PermissionCallback (res);
	}

}
