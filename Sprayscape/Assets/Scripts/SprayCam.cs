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

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;

// NOTE: specifically avoiding INotifyPropertyChange due to allocations for event args...

public interface ISpray
{
	bool Saved { get; }
	int Id { get; }
	int Order { get; set; }
	string Path { get; }
	Texture2D Texture { get; }
	Texture2D Thumbnail { get; }
	string Label { get; }
	string DriveFileName { get; }
	string ShareSlug { get; set; }
	string DriveFileId { get; set; }
	event Action<ISpray, int> OrderChanged;
	void LoadTexture();
	void UnloadTexture();
}

public class SavedSpray : ISpray
{
	private int id = -1;
	private int order = -1;
	private string label;
	private Texture2D texture;
	private Texture2D thumbnail;

	public SavedSpray(int id, int order)
	{
		this.id = id;
		this.order = order;
		this.label = PhotoIO.PhotoLabel(id);
		this.thumbnail = PhotoIO.thumbnails;
	}

	public int Id { get { return id; } }
	public int Order
	{
		get { return order; }
		set
		{
			if (order != value)
			{
				this.order = value;
				if (OrderChanged != null)
					OrderChanged(this, value);
			}
		}
	}
	public string Path { get { return PhotoIO.Path(id); } }
	public bool Saved { get { return id != -1; } }
	public string Label { get { return label; } }
	public Texture2D Texture { get { return texture; } }
	public Texture2D Thumbnail { get { return thumbnail; } }
	public string ShareSlug
	{
		get{
			return PlayerPrefs.GetString (id.ToString()+"_http");
		}
		set {
			PlayerPrefs.SetString (id.ToString()+"_http", value);
		}
	}

	public string DriveFileId
	{
		get{
			return PlayerPrefs.GetString (id.ToString()+"_id");
		}
		set {
			PlayerPrefs.SetString (id.ToString()+"_id", value);
		}
	}

	public string DriveFileName
	{
		get
		{
			string path = PhotoIO.Path(id);
			DateTime d = File.GetLastWriteTime(path);
			return string.Format("Sprayscape {0:D4}/{1:D2}/{2:D2} {3:D2}:{4:D2} {5}.jpg", d.Year, d.Month, d.Day, d.Hour, d.Minute, d.ToString("tt"));
		}
	}

	public event Action<ISpray, int> OrderChanged;

	public void LoadTexture()
	{
		if (texture == null)
		{
			texture = new Texture2D(1, 1, TextureFormat.ARGB32, false); // dummy texture so we can load into it
			PhotoIO.LoadImage(ref texture, id);
		}
	}

	public void UnloadTexture()
	{
		if (texture != null)
		{
			UnityEngine.Object.Destroy(texture);
			texture = null;
		}
	}
}

public enum CameraFacing
{
	Back,
	Front,
}

public interface ISprayCam
{
	List<ISpray> SavedSprays { get; }
	ISpray CurrentSpray { get; }

	void ClearWorkInProgress();
	ISpray SaveWorkInProgress();
	void DeleteSpray(ISpray spray);
	void ShareSprayAsImage(ISpray spray);
	void SprayStart();
	void SprayEnd();
	void OnboardingComplete();
	void PreviewSpray(ISpray spray);
	void EditSpray(ISpray spray);
	void ToggleBrushSize();
	void ToggleCameraFacing();

	bool ShouldShowOnBoarding { get; }
	BrushSize BrushSize { get; set; }
	CameraFacing CameraFacing { get; set; }

	event Action<BrushSize> BrushSizeChanged;
	event Action<CameraFacing> CameraFacingChanged;
	event Action OnboardingCompleted;
	event Action WorkInProgressCleared;
	event Action<ISpray> SprayCreated;
	event Action<ISpray> SprayDeleted;
	event Action<ISpray, bool> SprayShared;
	event Action SprayStarted;
	event Action Spraying;
	event Action SprayEnded;
}

public enum LinkShareBehaviour
{
	NativeDeviceShare,
	OpenBrowser
}

public class SprayCam : MonoBehaviour, ISprayCam
{
	private static readonly string ONBOARDING_COMPLETE_KEY = "OnBoardingComplete";

	public CameraCapture cameraCapture;
	public ViewManager viewManager;
	public DriveReceiver driveReceiver;
	public NativeShare nativeShare;

	public bool forceOnboardingOn = true;
	public string driveFolderName = "Sprayscapes";
	public string serviceUrl = "https://sprayscape.com/api/spheres";
	public string shareLinkFormat = "https://lh3.googleusercontent.com/d/{0}";
	public string shareLinkTitle = "";
	public string sharePopupTitle = "Share this Scape";
	public bool fakeLinkGenerationFailure = false;
	public bool forceAuthConfirm = false;
	public LinkShareBehaviour linkShareBehaviour = LinkShareBehaviour.OpenBrowser;
	public bool fakeContactFailure = true;
	public bool fakeNoGyro = false;

	private bool inWIP = true;
	private bool wipTextureBlank;
	private Texture2D wipTextureBackup;
	private ISpray currentSpray;
	private BrushSize brushSize = BrushSize.Big;
	private CameraFacing cameraFacing = CameraFacing.Back;
	private List<ISpray> sprays = new List<ISpray>(1024);
	private bool spraying = false;


	public GoogleAnalyticsV3 googleAnalytics;

	public string permissionsExplanationTitle = "Permissions Required";
	public string permissionsExplanationMessage = "Sprayscape requires camera and contacts permissions. The following screens will ask for any permissions that haven't been granted yet.";
	public string cameraPermissionRequiredTitle = "Camera Permission Required";
	public string cameraPermissionRequiredMessage = "Permission to use the camera is required in order to use this application.";
	public string storagePermissionRequiredTitle = "Storage Access Permission Required";
	public string storagePermissionRequiredMessage = "Access to external storage is a required permission in order to use this application.";
	public string contactsPermissionRequiredTitle = "Contacts Permission Required";
	public string contactsPermissionRequiredMessage = "Access to the contacts is a required permission in order to use this application.";

	#if UNITY_IOS
	public string permissionInstructionsAfterNeverAskAgainOrHomeOut = "If you pressed 'Deny', please provide this permission directly in the Settings > Privacy > Camera.";
	#else
	public string permissionInstructionsAfterNeverAskAgainOrHomeOut = "If you pressed 'Never ask again', please provide this permission directly in the Application Manager.";
	#endif

	public bool? hasCameraPermission = null, hasStoragePermission = null, hasContactsPermission = null;
	private bool? dialogClosed = null;

	public bool showMessageIfNoGyro = true;
	public GameObject noGyroPanel;
	
	public bool AllPermissionsGranted { get; private set; }

	void Awake()
	{
		if (cameraCapture == null)
			cameraCapture = FindObjectOfType<CameraCapture>();

		if (viewManager == null)
			viewManager = FindObjectOfType<ViewManager>();

		if (driveReceiver == null)
			driveReceiver = FindObjectOfType<DriveReceiver>();

		if (cameraCapture == null)
			throw new MissingReferenceException("Missing reference to CameraCapture: " + this);

		if (viewManager == null)
			throw new MissingReferenceException("Missing reference to ViewManager: " + this);

		if (driveReceiver == null)
			throw new MissingReferenceException("Missing reference to DriveReceiver: " + this);
	
		#if UNITY_EDITOR
		AllPermissionsGranted = true;
		#else
		AllPermissionsGranted = false;
		#endif
		
		Blank = true;
		LoadSprays();
	}

	void Start()
	{
		bool hasRequiredHardware = SystemInfo.supportsGyroscope && SystemInfo.supportsAccelerometer;
		if (Application.platform == RuntimePlatform.WindowsEditor)
			hasRequiredHardware = true;

		if (fakeNoGyro || !hasRequiredHardware)
		{
			viewManager.ShowNoGyro(); // this effectively disabled the app by putting user on a screen they can't change or do anything on
		}
		else
		{
			viewManager.ShowDefaultView(true);
			if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
				StartCoroutine(RequestPermissions());
		}
	}

	void OnEnable()
	{
		PermissionCallbackReceiver.PermissionRequestStatus += PermissionCallbackReceiver_PermissionRequestStatus;
		PermissionCallbackReceiver.DialogClosed += PermissionCallbackReceiver_DialogClosed;
	}
	void OnDisable()
	{
		PermissionCallbackReceiver.PermissionRequestStatus -= PermissionCallbackReceiver_PermissionRequestStatus;
		PermissionCallbackReceiver.DialogClosed -= PermissionCallbackReceiver_DialogClosed;
	}

	private void PermissionCallbackReceiver_DialogClosed()
	{
		dialogClosed = true;
	}
	private void PermissionCallbackReceiver_PermissionRequestStatus(string permission, bool granted)
	{
		if (permission == PermissionCallbackReceiver.CAMERA_PERMISSION)
			hasCameraPermission = granted;
		if (permission == PermissionCallbackReceiver.STORAGE_PERMISSION)
			hasStoragePermission = granted;
		if (permission == PermissionCallbackReceiver.CONTACTS_PERMISSION)
			hasContactsPermission = granted;
	}

	IEnumerator RequestPermissions()
	{
		// wait for the first frame...
		yield return null;

		var receiver = PermissionCallbackReceiver.GetPermissionCallbackReceiver();

		if (Debug.isDebugBuild)
			Debug.Log("Showing permissions explanation...");

		if (Debug.isDebugBuild)
			Debug.Log("Requesting CAMERA permission...");
		receiver.EnsureRequiredPermission(PermissionCallbackReceiver.CAMERA_PERMISSION, cameraPermissionRequiredTitle, cameraPermissionRequiredMessage, permissionInstructionsAfterNeverAskAgainOrHomeOut, () =>
			{
				hasCameraPermission = true;
				googleAnalytics.LogEvent("Permissions", "Camera", "True", 1);
			});

		while (!hasCameraPermission.HasValue || hasCameraPermission.Value == false)
			yield return null;

		if (Debug.isDebugBuild)
			Debug.Log("All permissions granted.");

		AllPermissionsGranted = true;
	}

	void LoadSprays()
	{
		PhotoIO.LoadFilePaths();

		for (int i = 0; i < PhotoIO.saveMap.Count; i++)
		{
			var s = new SavedSpray(PhotoIO.saveMap[i], i);
			sprays.Add(s);
		}

		PhotoIO.LoadThumbnailImage();
	}

	void CleanupWipTexture()
	{
		Debug.Log("Clearing WIP texture");
		if (wipTextureBackup != null)
		{
			UnityEngine.Object.Destroy(wipTextureBackup);
			wipTextureBackup = null;
		}
	}

	#region Properties

	public BrushSize BrushSize
	{
		get { return brushSize; }
		set
		{
			if (brushSize != value)
			{
				brushSize = value;
				switch (brushSize)
				{
				case BrushSize.Big:
					cameraCapture.SetBigCamSize();
					break;
				case BrushSize.Medium:
					cameraCapture.SetMedCamSize();
					break;
				case BrushSize.Small:
					cameraCapture.SetSmallCamSize();
					break;
				}
				Debug.Log("New brush size set: " + brushSize);
				if (BrushSizeChanged != null)
					BrushSizeChanged(value);
			}
		}
	}

	public CameraFacing CameraFacing
	{
		get { return cameraFacing; }
		set
		{
			if (cameraFacing != value)
			{
				cameraFacing = value;
				cameraCapture.SetCameraDirection(cameraFacing == CameraFacing.Front);
				Debug.Log("Camera facing set: " + cameraFacing);
				if (CameraFacingChanged != null)
					CameraFacingChanged(value);
			}
		}
	}

	public ISpray CurrentSpray { get { return currentSpray; } }
	public List<ISpray> SavedSprays { get { return sprays; } }
	public bool ShouldShowOnBoarding
	{
		get
		{
			if (forceOnboardingOn)
			{
				Debug.LogWarning("OnBoarding force on in editor, make sure this is turned on in release!");
				return true;
			}

			int onboardingComplete = PlayerPrefs.GetInt(ONBOARDING_COMPLETE_KEY, 0);
			Debug.LogWarning("OnBoardingComplete player prefs value: " + onboardingComplete);
			return onboardingComplete == 0;
		}
	}

	/// <summary>
	/// True if this is a new, blank spray, or if the current spray was just cleared.
	/// </summary>
	public bool Blank { get; private set; }

	#endregion

	#region Events

	public event Action<BrushSize> BrushSizeChanged;
	public event Action<CameraFacing> CameraFacingChanged;
	public event Action OnboardingCompleted;
	public event Action<ISpray> SprayCreated;
	public event Action<ISpray> SprayDeleted;
	public event Action<ISpray, bool> SprayShared;
	public event Action WorkInProgressCleared;
	public event Action SprayStarted;
	public event Action Spraying;
	public event Action SprayEnded;

	#endregion

	#region API Methods

	public void ClearWorkInProgress()
	{
		Debug.Log("Clearing Work in Progress");
		CleanupWipTexture();
		cameraCapture.Clear();
		Blank = true;
		inWIP = true;
		currentSpray = null;

		googleAnalytics.LogEvent("Spray", "Cleared", null, 1);

		if (WorkInProgressCleared != null)
			WorkInProgressCleared();
	}

	public void DeleteSpray(ISpray spray)
	{
		Debug.Log("Deleting spray");
		spray.UnloadTexture();
		sprays.Remove(spray);

		PhotoIO.DeleteImage(spray.Id);

		googleAnalytics.LogEvent("Spray", "Delete", spray.Id.ToString(), 1);
		// we need to update the spray ordering so the list view can update
		// for now just update them all, only ones that change will fire a change event
		for (int i = 0; i < sprays.Count; i++)
			sprays[i].Order = i;

		Blank = true;

		if (SprayDeleted != null)
			SprayDeleted(spray);
	}

	public void OnboardingComplete()
	{
		Debug.Log("Onboarding Complete, writing to player prefs");
		PlayerPrefs.SetInt(ONBOARDING_COMPLETE_KEY, 1);

		if (OnboardingCompleted != null)
			OnboardingCompleted();
	}

	public int GetSprayCount(){
		return sprays.Count;
	}

	public ISpray SaveWorkInProgress()
	{
		Debug.Log("Saving WIP");

	

		int id = cameraCapture.Save(); // automatically clears the current render texture
		var spray = new SavedSpray(id, PhotoIO.saveMap.Count - 1);
		sprays.Add(spray);
		// so we can access it after saving
		currentSpray = spray;

		googleAnalytics.LogEvent("Spray", "Save Work In Progress", spray.Id.ToString(), 1);

		if (SprayCreated != null)
			SprayCreated(spray);

		return spray;
	}

	public void PreviewSpray(ISpray spray)
	{
		Debug.Log("Previewing spray");

		if (inWIP == true)
		{
			CleanupWipTexture();
			wipTextureBlank = Blank;
			wipTextureBackup = cameraCapture.CopyToNewTexture();
			wipTextureBackup.Apply();
		}
		else
		{
			wipTextureBlank = false;
		}
		Blank = false;
		inWIP = false;
		currentSpray = spray;
		spray.LoadTexture();
		cameraCapture.LoadImage(spray.Texture);
		spray.UnloadTexture();

		googleAnalytics.LogEvent("Spray", "Preview Spray", spray.Id.ToString(), 1);

	}

	public void RestoreWipTexture()
	{
		Debug.Log("Restoring WIP");
		if (currentSpray != null)
		{
			currentSpray.UnloadTexture();
		}
		if (wipTextureBackup != null)
		{
			//Color[] colors = wipTextureBackup.GetPixels();
			cameraCapture.Clear();
			cameraCapture.LoadImage(wipTextureBackup);
		}
		Blank = wipTextureBlank;
		inWIP = true;
		currentSpray = null;
	}

	public void EditSpray(ISpray spray)
	{
		Debug.Log("Editing spray as new spray");
		CleanupWipTexture();
		inWIP = true;
		if (currentSpray != spray)
		{
			spray.LoadTexture();
			cameraCapture.LoadImage(spray.Texture);
			spray.UnloadTexture();

			googleAnalytics.LogEvent("Spray", "Edit Spray", spray.Id.ToString(), 1);
		}
		currentSpray = null;
	}

	public void ShareSprayAsImage(ISpray spray)
	{
		Debug.Log("Share spray as Image");

		viewManager.ShowFacebookShareBackground();
		nativeShare.ShareImage(spray.Path);

		if (SprayShared != null)
			SprayShared(spray, true);
	}

	public void ShareSprayAsString(String link){
		viewManager.ShowBlankShareBackground();
		nativeShare.ShareUrl(link, shareLinkTitle, sharePopupTitle);
	}

	public void OpenSprayInBrowser(String link){
		Application.OpenURL(link);
	}

	public void CopyLinkToClipboard(String link){
		
	}

	private void ShareLink(string link)
	{
		
		switch (linkShareBehaviour)
		{
			case LinkShareBehaviour.NativeDeviceShare:
//				nativeShare.ShareUrl(link, shareLinkTitle, sharePopupTitle);
//				GUIUtility.systemCopyBuffer = link;
//				viewManager.ShowShareLinkChoice (link);
				ShareSprayAsString(link);
				break;
			case LinkShareBehaviour.OpenBrowser:
				Application.OpenURL(link);
				break;
			default:
				Application.OpenURL(link);
				break;
		}

		//TODO: Popup share decision box;
		//----------------------------


	}



	private IEnumerator GetShareLink(ISpray spray, String fileId)
	{
		
		if (spray.ShareSlug.Contains ("http")) {

			//SHARE LINK HERE ------------------
			ShareLink (spray.ShareSlug);
			// ---------------------------------

			if (SprayShared != null) SprayShared (spray, false);
			
		} else {



			string shareLink = String.Format (shareLinkFormat, fileId);
			Debug.Log ("Spray drive fileId in unity land: " + fileId + " shareLink: " + shareLink);
			string hexHash;
			using (MD5 md5 = MD5.Create ()) {
				string md5Source = Secrets.shareSecret + shareLink;
				byte[] encodedSource = Encoding.UTF8.GetBytes (md5Source);
				byte[] hashBytes = md5.ComputeHash (encodedSource);
				StringBuilder sb = new StringBuilder ();
				for (int i = 0; i < hashBytes.Length; i++) {
					sb.Append (hashBytes [i].ToString ("x2")); // 2-digit hex per byte
				}

				hexHash = sb.ToString ();
			}

			// Get OAuth2 access token for google api
			string token = driveReceiver.GetAccessToken();
		
			Debug.Log ("hex hash for sig: " + hexHash);

			WWWForm f = new WWWForm ();
			f.AddField ("url", shareLink);
			f.AddField ("sig", hexHash);
			f.AddField ("token", token);

			WWW w = new WWW (serviceUrl, f);
			yield return w;

			if (string.IsNullOrEmpty (w.error) && !fakeLinkGenerationFailure) {
				Debug.Log (w.text);
				string shareUrl = w.text.Trim ();

				//SHARE LINK HERE ------------------
				ShareLink (shareUrl);
				// ---------------------------------

				if (SprayShared != null)
					SprayShared (spray, false);

				spray.ShareSlug = shareUrl;

				if (linkShareBehaviour == LinkShareBehaviour.OpenBrowser) {
					viewManager.Back (); // pops off 'uploading view'
					viewManager.Back (); // pops off the share screen
				}
			} else {
				if (fakeLinkGenerationFailure)
					Debug.LogError ("Faking link generation failure");
				else {
					Debug.LogError (w.error);
					Debug.LogError (w.text);

				}
				viewManager.ShowUploadFailed (true);
			}
		}
	}


	private Boolean permissionsResult;
	IEnumerator CheckSharePermissionsCoroutine()
	{
		viewManager.ShowUploadingView ();
		yield return new WaitForSeconds(0.3f);

		permissionsResult = true;

		// first we need to check for permissions...
		var receiver = PermissionCallbackReceiver.GetPermissionCallbackReceiver();

		if (Application.platform == RuntimePlatform.Android) {
			// Check for CONTACTS permissions
			if (receiver.HasPermission (PermissionCallbackReceiver.CONTACTS_PERMISSION)) {
				Debug.Log ("CONTACTS permission already granted");
			} else {
				hasContactsPermission = null;
				Debug.Log ("Requesting CONTACTS permission");

				// this will set hasContactsPermission to true or false
				receiver.RequestPermission (PermissionCallbackReceiver.CONTACTS_PERMISSION);

				// wait for positive or negative response
				while (!hasContactsPermission.HasValue)
					yield return null;

				if (hasContactsPermission.Value == false) {
					// the user denied the contacts permision...
					viewManager.ShowContactPermissionErrorView ();
					permissionsResult = false;
					yield break; // stop the co-routine
				}
			}

			// Create a DrivePermissionsResult to sattisfy compiler
			DrivePermissionsResult drivePermissionResult = new DrivePermissionsResult
			{
				failed = true,
				failedReason = "No result",
				failureType = DriveFailureType.GenericFailure,
			};

			// Check permissions on Google Drive
			yield return driveReceiver.CheckPermissionsCoroutine ((DrivePermissionsResult result) => {
				drivePermissionResult.failed = result.failed;
				drivePermissionResult.failedReason = result.failedReason;
				drivePermissionResult.failureType = result.failureType;
				drivePermissionResult.accountName = result.accountName;
			});


			if (drivePermissionResult.failed)
			{
				Debug.LogError("Spray drive permission failed: " + drivePermissionResult);
				googleAnalytics.LogEvent("Spray", "Share error", "Spray drive permission failed: " + drivePermissionResult, 1);

				switch (drivePermissionResult.failureType)
				{
				case DriveFailureType.DrivePermissionIssue:
					viewManager.ShowAccountProhibited();
					break;
				case DriveFailureType.NoConnection:
					viewManager.ShowBadConnection(true);
					break;
				case DriveFailureType.AuthCanceled:
					viewManager.ShowDriveAuthFailed(true);
					break;
				case DriveFailureType.AuthFailed:
					viewManager.ShowDriveAuthFailed(true);
					break;
				default:
					// generic error handling for now
					viewManager.ShowUploadFailed(true);
					break;
				}
				permissionsResult = false;
			}
		} else if (Application.platform == RuntimePlatform.IPhonePlayer) {

			// Create a DrivePermissionsResult to sattisfy compiler
			DrivePermissionsResult drivePermissionResult = new DrivePermissionsResult
			{
				failed = true,
				failedReason = "No result",
				failureType = DriveFailureType.GenericFailure,
			};

			// Check permissions on Google Drive
			yield return driveReceiver.CheckPermissionsCoroutine ((DrivePermissionsResult result) => {
				drivePermissionResult.failed = result.failed;
				drivePermissionResult.failedReason = result.failedReason;
				drivePermissionResult.failureType = result.failureType;
				drivePermissionResult.accountName = result.accountName;
			});

			if (drivePermissionResult.failed)
			{
				Debug.LogError("Spray drive permission failed: " + drivePermissionResult);
				googleAnalytics.LogEvent("Spray", "Share error", "Spray drive permission failed: " + drivePermissionResult, 1);

				switch (drivePermissionResult.failureType)
				{
				case DriveFailureType.DrivePermissionIssue:
					viewManager.ShowAccountProhibited();
					break;
				case DriveFailureType.NoConnection:
					viewManager.ShowBadConnection(true);
					break;
				case DriveFailureType.AuthCanceled:
					viewManager.ShowDriveAuthFailed(true);
					break;
				default:
					// generic error handling for now
					viewManager.ShowUploadFailed(true);
					break;
				}
				permissionsResult = false;
			}
		}
		else
		{
			if (fakeContactFailure)
			{
				Debug.Log("Faking contact failure in editor");
				viewManager.ShowContactPermissionErrorView();
				permissionsResult = false;
				yield break; // stop the co-routine
			}
			else
			{
				Debug.Log("Skipping CONTACTS permission request on this platform: " + Application.platform.ToString());
			}
		}
	}

	// Upload specific spray to drive
	IEnumerator UploadImageCoroutine(ISpray spray)
	{
		
		DriveUploadResult driveUploadResult = new DriveUploadResult
		{
			failed = true,
			failedReason = "No result",
			failureType = DriveFailureType.GenericFailure,
			fileId = null,
		};
		;

		yield return driveReceiver.UploadCoroutine(driveFolderName, spray.DriveFileName, spray.Path, (DriveUploadResult result) =>
		{
				driveUploadResult.failed = result.failed;
				driveUploadResult.failedReason = result.failedReason;
				driveUploadResult.failureType = result.failureType;
				driveUploadResult.fileId = result.fileId;
		});

		
		if (driveUploadResult.failed)
		{
			Debug.LogError("Spray drive upload failed, but at least we got notified in Unity: " + driveUploadResult);
			googleAnalytics.LogEvent("Spray", "Share error", "Spray drive upload failed, but at least we got notified in Unity: " + driveUploadResult, 1);

			switch (driveUploadResult.failureType)
			{
				case DriveFailureType.DrivePermissionIssue:
					viewManager.ShowAccountProhibited();
					break;
				case DriveFailureType.NoConnection:
					viewManager.ShowBadConnection(true);
					break;
				case DriveFailureType.AuthCanceled:
					viewManager.ShowDriveAuthFailed(true);
					break;
				default:
					// generic error handling for now
					viewManager.ShowUploadFailed(true);
					break;
			}
		}
		else
		{
			spray.DriveFileId = driveUploadResult.fileId;
			yield return GetShareLink(spray, driveUploadResult.fileId);
		}
	}

	// Coroutine handling sharing a spray. 
	// Checks for permissions, checks if spray is on drive already, uploads and creates share url
	public IEnumerator ShareSprayAsLink(ISpray spray)
	{
		Debug.Log("Share spray as Link");

		// Check permissions to share
		yield return StartCoroutine (CheckSharePermissionsCoroutine ());
		Debug.Log("Permissions checked "+ permissionsResult);

		if (permissionsResult == true) {
			// Check if file already exists
			Boolean fileExists = false;
			Debug.Log ("Check drive file id " + spray.DriveFileId);

			if (spray.DriveFileId != null && spray.DriveFileId.Length > 0) {

				// Create a DrivePermissionsResult to sattisfy compiler
				DriveFileExistsResult driveFileExistsResult = new DriveFileExistsResult
				{
					failed = true,
					exists = false,
					failedReason = "No result",
					failureType = DriveFailureType.GenericFailure,
				};

				// Check permissions on Google Drive
				yield return driveReceiver.CheckFileId (spray.DriveFileId, (DriveFileExistsResult result) => {
					driveFileExistsResult.failed = result.failed;
					driveFileExistsResult.exists = result.exists;
					driveFileExistsResult.failedReason = result.failedReason;
					driveFileExistsResult.failureType = result.failureType;
					driveFileExistsResult.accountName = result.accountName;
				});
				fileExists = driveFileExistsResult.exists;

				if (driveFileExistsResult.failed)
				{
					Debug.LogError("Spray drive check file id failed: " + driveFileExistsResult);
					googleAnalytics.LogEvent("Spray", "Share error", "Spray drive check file id failed: " + driveFileExistsResult, 1);

					switch (driveFileExistsResult.failureType)
					{
					case DriveFailureType.DrivePermissionIssue:
						viewManager.ShowAccountProhibited();
						break;
					case DriveFailureType.NoConnection:
						viewManager.ShowBadConnection(true);
						break;
					case DriveFailureType.AuthCanceled:
						viewManager.ShowDriveAuthFailed(true);
						break;
					default:
						// generic error handling for now
						viewManager.ShowUploadFailed(true);
						break;
					}
					fileExists = false;
				}
			}

			// Check if shareslug exists, otherwise upload
			if (spray.ShareSlug.Contains ("http") && fileExists) {
			
				//SHARE LINK HERE ------------------
				ShareLink (spray.ShareSlug);
				// ---------------------------------
				if (SprayShared != null)
					SprayShared (spray, false);

			} else {
				// Clear the shareslug
				spray.ShareSlug = "";
					
				// Upload file
				yield return StartCoroutine (UploadImageCoroutine (spray));
			}
		}
	}

	private float startTime = 0;

	public void SprayStart()
	{
		spraying = true;

		startTime = Time.fixedTime;

		googleAnalytics.LogEvent("Spray", "Start Spraying", "camera-facing: " + cameraFacing.ToString() + " brush-size: " + brushSize.ToString(), 1);

		if (SprayStarted != null)
			SprayStarted();

	}

	public void SprayEnd()
	{
		spraying = false;

		float elapsedTime = Time.fixedTime - startTime;

		googleAnalytics.LogEvent("Spray", "Stop Spraying", "camera-facing: " + cameraFacing.ToString() + " brush-size: " + brushSize.ToString(), 1);
		googleAnalytics.LogTiming("Spray Length", Convert.ToInt64(elapsedTime), "camera-facing: " + cameraFacing.ToString(), "brush-size: " + brushSize.ToString());
		if (SprayEnded != null)
			SprayEnded();
	}


	public void ToggleBrushSize()
	{
		switch (brushSize)
		{
		case BrushSize.Big:
			BrushSize = BrushSize.Medium;
			break;
		case BrushSize.Medium:
			BrushSize = BrushSize.Small;
			break;
		case BrushSize.Small:
			BrushSize = BrushSize.Big;
			break;
		}
	}


	public void ToggleCameraFacing()
	{
		CameraFacing = cameraFacing == CameraFacing.Front ? CameraFacing.Back : CameraFacing.Front;
	}

	public void OnActivityResult(int requestCode, int resultCode, string intentAsString)
	{
		// we will let the view manager handle this for now...
	}

	#endregion

	#region Update

	void Update()
	{

		if (spraying)
		{
			bool sprayed = cameraCapture.Capture();

			if (sprayed)
			{
				Blank = false;

				if (Spraying != null)
					Spraying();
			}
		}
	}

	#endregion
}
