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

public enum DriveFailureType
{
	UploadInProgress,
	Timeout,
	GenericFailure,
	AuthCanceled,
	AuthFailed,
	DrivePermissionIssue, // (i.e. a google.com account was used...)
	NoConnection,
	AccountSelectionCanceled,
}

public struct DriveFileExistsResult 
{
	public bool failed;
	public bool exists;
	public string failedReason;
	public string accountName;
	public DriveFailureType failureType;

	public override string ToString()
	{
		if (failed)
		{
			return "DriveFileExistsResult: FAILED, " + failureType.ToString() + ", reason: " + failedReason;
		}
		else
		{
			return "DriveFileExistsResult: OK, " + exists + ", " + accountName;
		}
	}
}

public struct DrivePermissionsResult 
{
	public bool failed;
	public string failedReason;
	public string accountName;
	public DriveFailureType failureType;

	public override string ToString()
	{
		if (failed)
		{
			return "DrivePermissionsResult: FAILED, " + failureType.ToString() + ", reason: " + failedReason;
		}
		else
		{
			return "DrivePermissionsResult: OK, " + accountName;
		}
	}
}

public struct DriveUploadResult
{
	public bool failed;
	public string failedReason;
	public string fileId;
	public DriveFailureType failureType;

	public override string ToString()
	{
		if (failed)
		{
			return "DriveUploadResult: FAILED, " + failureType.ToString() + ", reason: " + failedReason;
		}
		else
		{
			return "DriveUploadResult: OK, " + fileId;
		}
	}
}

public class DriveReceiver : MonoBehaviour
{
	public float timeOutInSeconds = 30.0f;

	public string iOSKeychainName;
	public string iOSClientId;

	public bool fakeUploadResult = true;
	public bool fakeUploadFailed = true;
	public string fakeFileId = "";
	public string fakeUploadFailReason = "Because you are in Editor";
	public DriveFailureType fakeFailureType = DriveFailureType.GenericFailure;

	private bool uploading = false;
	private bool waitingForUpload = true;
	private bool waitingForPermissions = true;
	private bool waitingForFileCheck = true;
	private bool waitingForToken = true;
	private bool uploadFailed = false;
	private bool fileExists = false;
	private DriveFailureType failureType = DriveFailureType.GenericFailure;
	private string fileId;
	private string failedReason;
	private string accountName;
	private string token;

	#region Drive Events

	public void DriveAccountSelected(string accountName)
	{
		Debug.Log("DriveAccountSelected('" + accountName + "')");
		this.accountName = accountName;
		waitingForPermissions = false;
	}

	public void DriveTokenObtained(string token)
	{
		Debug.Log("DriveTokenObtained('" + token + "')");
		this.token = token;

		waitingForToken = false;
	}

	public void DriveAccountSelectionCanceled(string nothing)
	{
		Debug.Log("DriveAccountSelectionCanceled()");
		uploadFailed = true;
		failureType = DriveFailureType.AccountSelectionCanceled;
		waitingForPermissions = false;
	}

	public void DriveIsReady(string nothing)
	{
		Debug.Log("DriveIsReady()");
		waitingForPermissions = false;
	}

	public void DriveFileUploaded(string fileId)
	{
		Debug.Log("DriveFileUploaded('" + fileId + "')");
		this.fileId = fileId;
		waitingForUpload = false;
	}

	public void DriveFileExists(string exists)
	{
		Debug.Log("DriveFileExists('" + exists + "')");
		if (exists == "True") {
			fileExists = true;
		} else {
			fileExists = false;
		}
		waitingForFileCheck = false;
	}

	public void DriveAuthFailed(string reason)
	{
		Debug.Log("DriveAuthFailed('" + reason + "')");
		uploadFailed = true;
		failedReason = reason;
		failureType = DriveFailureType.AuthFailed;
		waitingForUpload = false;
		waitingForPermissions = false;
		waitingForToken = false;
	}

	public void DriveAuthCanceled(string reason)
	{
		Debug.Log("DriveAuthCanceled('" + reason + "')");
		uploadFailed = true;
		failedReason = reason;
		failureType = DriveFailureType.AuthCanceled;
		waitingForUpload = false;
		waitingForPermissions = false;
		waitingForToken = false;
	}

	public void DrivePermissionChangeFailed(string reason)
	{
		
		Debug.Log("DrivePermissionChangeFailed('" + reason + "')");
		// we will still get the generic DriveUploadFailed() event as well, so don't stop the co-routine yet!
		//waitingForUpload = false;
		uploadFailed = true;
		failedReason = reason;
		failureType = DriveFailureType.DrivePermissionIssue;
	}

	public void DriveNotOnline(string reason)
	{
		Debug.Log("DriveNotOnline('" + reason + "')");
		// we will still get the generic DriveUploadFailed() event as well, so don't stop the co-routine yet!
		//waitingForUpload = false;
		uploadFailed = true;
		failedReason = reason;
		failureType = DriveFailureType.NoConnection;
		waitingForPermissions = false;
	}


	public void DriveUploadFailed(string reason)
	{
		Debug.Log("DriveUploadFailed('" + reason + "')");
		waitingForUpload = false;
		uploadFailed = true;
		failedReason = reason;
		// NOTE: purposefully don't set this here, since DrivePermissionChangeFailed may have already set it
		//failureType = DriveFailureType.GenericFailure;
	}


	private void DummyPermissionsResultCallback(DrivePermissionsResult r) { }
	private void DummyResultCallback(DriveUploadResult r) { }

	public string GetDriveEmail(){
		return accountName;
	}

	public string GetAccessToken(){
		return token;
	}

	// Check if drive fileId still exists on drive
	public IEnumerator CheckFileId(string fileId, System.Action<DriveFileExistsResult> resultCallback){
		Debug.Log ("CheckFileId('" + fileId + "')");

		bool fileExists = false;
		if (Application.platform == RuntimePlatform.Android)
		{
			#if UNITY_ANDROID
			using (AndroidJavaClass activityClass = new AndroidJavaClass("com.androidexperiments.sprayscape.unitydriveplugin.GoogleDriveUnityPlayerActivity"))
			{
				using (AndroidJavaObject activity = activityClass.GetStatic<AndroidJavaObject>("activityInstance"))
				{
					fileExists = activity.Call<bool>("checkFileId", fileId);

					resultCallback(new DriveFileExistsResult
						{
							failed = false,
							exists = fileExists
						});
					
				}
			}
			#endif
		}
		else if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			fileExists = false;
			#if UNITY_IOS
			_GoogleDrivePlugin_CheckFileId(fileId, this.gameObject.name, this.iOSClientId, this.iOSKeychainName);
			#endif
		}

		float startTime = Time.time;

		while (waitingForFileCheck)
		{
			// check for timeout
			float ellapsed = Time.time - startTime;
			if (ellapsed > timeOutInSeconds)
			{
				// force a time-out here, seems like java-land is not getting back to us...
				waitingForFileCheck = false;
				uploadFailed = true;
				failedReason = "Operation timed-out";
				failureType = DriveFailureType.Timeout;
			}

			yield return null;
		}

		resultCallback(new DriveFileExistsResult
			{
				failed = this.uploadFailed,
				exists = fileExists,
				failedReason = this.failedReason,
				failureType = this.failureType
			});
	}

	// Coroutine checking for all required permissions and checks if account is selected
	public IEnumerator CheckPermissionsCoroutine( System.Action<DrivePermissionsResult> resultCallback)
	{
		Debug.Log ("CheckPermissionsCoroutine()");
			
		if (resultCallback == null)
			resultCallback = DummyPermissionsResultCallback;

		uploadFailed = false;
		failureType = DriveFailureType.GenericFailure;
		waitingForPermissions = true;

		if (Application.platform == RuntimePlatform.Android)
		{
			#if UNITY_ANDROID
			using (AndroidJavaClass activityClass = new AndroidJavaClass("com.androidexperiments.sprayscape.unitydriveplugin.GoogleDriveUnityPlayerActivity"))
			{
				using (AndroidJavaObject activity = activityClass.GetStatic<AndroidJavaObject>("activityInstance"))
				{
					activity.Call<bool>("checkAccountPermissions", this.gameObject.name);
				}
			}
			#endif
		}
		else if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			#if UNITY_IOS
			_GoogleDrivePlugin_CheckPermissions(this.gameObject.name, this.iOSClientId, this.iOSKeychainName);
			#endif
		}


		float startTime = Time.time;

		while (waitingForPermissions)
		{
			// check for timeout
			float ellapsed = Time.time - startTime;
			if (ellapsed > timeOutInSeconds)
			{
				// force a time-out here, seems like java-land is not getting back to us...
				waitingForPermissions = false;
				uploadFailed = true;
				failedReason = "Operation timed-out";
				failureType = DriveFailureType.Timeout;
			}

			yield return null;
		}


		// Get token
		waitingForToken = true;

		if (Application.platform == RuntimePlatform.Android)
		{
			#if UNITY_ANDROID
			using (AndroidJavaClass activityClass = new AndroidJavaClass("com.androidexperiments.sprayscape.unitydriveplugin.GoogleDriveUnityPlayerActivity"))
			{
				using (AndroidJavaObject activity = activityClass.GetStatic<AndroidJavaObject>("activityInstance"))
				{
					activity.Call<bool>("fetchAuthToken", this.gameObject.name);
				}
			}
			#endif
		}
		else if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			#if UNITY_IOS
			_GoogleDrivePlugin_CheckPermissions(this.gameObject.name, this.iOSClientId, this.iOSKeychainName);
			#endif
		}


		startTime = Time.time;

		while (waitingForToken)
		{
			// check for timeout
			float ellapsed = Time.time - startTime;
			if (ellapsed > timeOutInSeconds)
			{
				// force a time-out here, seems like java-land is not getting back to us...
				waitingForToken = false;
				uploadFailed = true;
				failedReason = "Operation timed-out";
				failureType = DriveFailureType.Timeout;
			}

			yield return null;
		}
			
		resultCallback(new DrivePermissionsResult
			{
				failed = this.uploadFailed,
				failedReason = this.failedReason,
				failureType = this.failureType,
				accountName = accountName
			});
	}


	// Coroutine uploading a local file to drive
	public IEnumerator UploadCoroutine(string driveFolderName, string driveFileName, string localPath, System.Action<DriveUploadResult> resultCallback)
	{
		if (resultCallback == null)
			resultCallback = DummyResultCallback;

		if (uploading)
		{
			resultCallback(new DriveUploadResult
			{
				failed = true,
				failedReason = "Upload already in progress",
				fileId = null,
				failureType = DriveFailureType.UploadInProgress
			});
			yield break; // stop co-routine
		}

		uploading = true;
		waitingForUpload = true;
		uploadFailed = false;
		failureType = DriveFailureType.GenericFailure;
		fileId = null;

		if (Application.platform == RuntimePlatform.Android)
		{
#if UNITY_ANDROID
			using (AndroidJavaClass activityClass = new AndroidJavaClass("com.androidexperiments.sprayscape.unitydriveplugin.GoogleDriveUnityPlayerActivity"))
			{
				using (AndroidJavaObject activity = activityClass.GetStatic<AndroidJavaObject>("activityInstance"))
				{
					activity.Call<bool>("uploadFile", driveFolderName, driveFileName, localPath, this.gameObject.name);
				}
			}
#endif
		}
		else if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
#if UNITY_IOS
			_GoogleDrivePlugin_UploadFile(driveFolderName, driveFileName, localPath, this.gameObject.name, this.iOSClientId, this.iOSKeychainName);
#endif
		}
		else
		{
			// PC/Editor, use editor properties to fake the result so we can test both cases...
			yield return new WaitForSeconds(1.0f);
			waitingForUpload = false;
			uploadFailed = fakeUploadFailed;
			failedReason = fakeUploadFailReason;
			fileId = fakeFileId;
			failureType = fakeFailureType;
		}

		float startTime = Time.time;

		while (waitingForUpload)
		{
			// check for timeout
			float ellapsed = Time.time - startTime;
			if (ellapsed > timeOutInSeconds)
			{
				// force a time-out here, seems like java-land is not getting back to us...
				waitingForUpload = false;
				uploadFailed = true;
				failedReason = "Operation timed-out";
				fileId = null;
				failureType = DriveFailureType.Timeout;
			}

			yield return null;
		}

		// set this before the call back just in case
		uploading = false;

		resultCallback(new DriveUploadResult
		{
			failed = this.uploadFailed,
			failedReason = this.failedReason,
			fileId = this.fileId,
			failureType = this.failureType,
		});
	}

	#endregion
#if UNITY_IOS
	[DllImport("__Internal")]
	private static extern void _GoogleDrivePlugin_UploadFile (
		string driveFolderName,
		string driveFileName,
		string localPath,
		string callbackObjectName,
		string clientId,
		string keychainName
	);
	[DllImport("__Internal")]
	private static extern bool _GoogleDrivePlugin_CheckFileId (
		string fileId,
		string callbackObjectName,
		string clientId,
		string keychainName
	);
	[DllImport("__Internal")]
	private static extern bool _GoogleDrivePlugin_CheckPermissions (
		string callbackObjectName,
		string clientId,
		string keychainName
	);

#endif
}
