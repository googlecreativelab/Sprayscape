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
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Text;

public enum BrushSize
{
	Small,
	Medium,
	Big
}

public interface ISprayCan
{
	BrushSize BrushSize { get; set; }
}

public interface IView
{
	string Name { get; }
	GameObject[] ManageActive { get; }

	void InView(IView previousView);
	void OutOfView(IView newView);
}

public interface IAnimatedView : IView
{
	Animator[] Animators { get; }
	float InTime { get; }
	float OutTime { get; }
	string InTrigger { get; }
	string OutTrigger { get; }

	void EnteringView(IView previousView);
	void EnteringPercent(float percent);
	void LeavingView(IView newView);
	void LeavingPercent(float percent);
}

public enum ShowMode
{
	PushView,
	ReplaceTop,
	Back,
}

public class ViewManager : MonoBehaviour
{
	private const int _SPRAY_LIMIT = 128;

	public SprayCam sprayCam;
	public CameraCapture cameraCapture;

	public GoogleAnalyticsV3 googleAnalytics;

	public LibraryController libaryController;

	public GameObject editViewObject;
	public GameObject listViewObject;
	public GameObject viewViewObject;
	public GameObject savingViewObject;
	public GameObject deleteViewObject;
	public GameObject clearViewObject;
	public GameObject saveCompleteViewObject;
	public GameObject timeToTidayViewObject;
	public GameObject googleViewObject;
	public GameObject shareChoiceObject;
	public GameObject shareLinkChoiceObject;
	public GameObject uploadingViewObject;
	public GameObject uploadFailedObject;
	public GameObject authConfirmObject;
	public GameObject aboutViewObject;
	public GameObject accountProhibitedObject;
	public GameObject facebookShareBackgroundObject;
	public GameObject blankShareBackgroundObject;
	public GameObject contactsPermissionErrorObject;
	public GameObject noGyroObject;
	public GameObject badConnectionObject;
	public GameObject driveAuthFailedObject;

	[Range(0, 1)]
	public float corssfadePercentage = 0.0f;
	public bool debugViewStack = true;

	private IAnimatedView[] views;

	private IAnimatedView editView;
	private IAnimatedView listView;
	private IAnimatedView viewView;
	private IAnimatedView savingView;
	private IAnimatedView deleteView;
	private IAnimatedView clearView;
	private IAnimatedView saveCompleteView;
	private IAnimatedView googleView;
	private IAnimatedView shareChoiceView;
	private IAnimatedView shareLinkChoiceView;
	private IAnimatedView timeToTidayView;
	private IAnimatedView uploadingView;
	private IAnimatedView uploadFailedView;
	private IAnimatedView authConfirmView;
	private IAnimatedView aboutView;
	private IAnimatedView accountProhibitedView;
	private IAnimatedView facebookShareBackgroundView;
	private IAnimatedView blankShareBackgroundView;
	private IAnimatedView contactsPermissionErrorView;
	private IAnimatedView noGyroView;
	private IAnimatedView badConnectionView;
	private IAnimatedView driveAuthFailedView;

	private IAnimatedView defaultView;
	private IAnimatedView currentView;
	private IAnimatedView nextView;
	private ShowMode nextViewShowMode;
	private bool transitionPending = false;

	private Stack<IAnimatedView> viewStack = new Stack<IAnimatedView>();

	#region Events

	public event Action ViewChanged;

	#endregion

	void Awake()
	{
		if (sprayCam == null)
			sprayCam = FindObjectOfType<SprayCam>();

		if (editViewObject == null)
			throw new MissingReferenceException("Edit view reference is missing on: " + this);

		if (listViewObject == null)
			throw new MissingReferenceException("List view reference is missing on: " + this);

		if (viewViewObject == null)
			throw new MissingReferenceException("View view reference is missing on: " + this);

		if (savingViewObject == null)
			throw new MissingReferenceException("Saving view reference is missing on: " + this);

		if (deleteViewObject == null)
			throw new MissingReferenceException("Delete view reference is missing on: " + this);

		if (clearViewObject == null)
			throw new MissingReferenceException("Clear view reference is missing on: " + this);

		if (saveCompleteViewObject == null)
			throw new MissingReferenceException("Save Complete view reference is missing on: " + this);

		if (timeToTidayViewObject == null)
			throw new MissingReferenceException("Time to Tidy view reference is missing on: " + this);

		if (googleViewObject == null)
			throw new MissingReferenceException("Google view reference is missing on: " + this);

		if (shareChoiceObject == null)
			throw new MissingReferenceException("Share Choice view reference is missing on: " + this);

		if (uploadingViewObject == null)
			throw new MissingReferenceException("Uploading view reference is missing on: " + this);

		// this is getting stupid....
		if (uploadFailedObject == null)
			throw new MissingReferenceException("Upload Failed view reference is missing on: " + this);

		if (authConfirmObject == null)
			throw new MissingReferenceException("Auth Confirm view reference is missing on: " + this);

		if (accountProhibitedObject == null)
			throw new MissingReferenceException("Account Prohibited view reference is missing on: " + this);

		if (aboutViewObject == null)
			throw new MissingReferenceException("About wiew reference is missing on: " + this);

		if (facebookShareBackgroundObject == null)
			throw new MissingReferenceException("Share Background view reference is missing on: " + this);

		if (blankShareBackgroundObject == null)
			throw new MissingReferenceException("Blank Background view reference is missing on: " + this);

		if (contactsPermissionErrorObject == null)
			throw new MissingReferenceException("Contacts Error view reference is missing on: " + this);

		if (noGyroObject == null)
			throw new MissingReferenceException("No gyro view reference is missing on: " + this);

		if (badConnectionObject == null)
			throw new MissingReferenceException("Bad Connection view reference is missing on: " + this);

		if (driveAuthFailedObject == null)
			throw new MissingReferenceException("Drive Auth Failed view reference is missing on: " + this);


		editView = editViewObject.GetComponent<IAnimatedView>();
		if (editView == null)
			throw new MissingComponentException("Edit view object is missing a component that implements IAnimatedView on: " + this);

		listView = listViewObject.GetComponent<IAnimatedView>();
		if (listView == null)
			throw new MissingComponentException("List view object is missing a component that implements IAnimatedView on: " + this);

		viewView = viewViewObject.GetComponent<IAnimatedView>();
		if (viewView == null)
			throw new MissingComponentException("View view object is missing a component that implements IAnimatedView on: " + this);

		savingView = savingViewObject.GetComponent<IAnimatedView>();
		if (savingView == null)
			throw new MissingComponentException("Saving view object is missing a component that implements IAnimatedView on: " + this);

		deleteView = deleteViewObject.GetComponent<IAnimatedView>();
		if (deleteView == null)
			throw new MissingComponentException("Delete view object is missing a component that implements IAnimatedView on: " + this);

		clearView = clearViewObject.GetComponent<IAnimatedView>();
		if (clearView == null)
			throw new MissingComponentException("Clear view object is missing a component that implements IAnimatedView on: " + this);

		saveCompleteView = saveCompleteViewObject.GetComponent<IAnimatedView>();
		if (saveCompleteView == null)
			throw new MissingComponentException("Save Complete view object is missing a component that implements IAnimatedView on: " + this);

		googleView = googleViewObject.GetComponent<IAnimatedView>();
		if (googleView == null)
			throw new MissingComponentException("Google view object is missing a component that implements IAnimatedView on: " + this);

		shareChoiceView = shareChoiceObject.GetComponent<IAnimatedView>();
		if (shareChoiceView == null)
			throw new MissingComponentException("Share Choice view object is missing a component that implements IAnimatedView on: " + this);

		shareLinkChoiceView = shareLinkChoiceObject.GetComponent<IAnimatedView>();
		if (shareLinkChoiceView == null)
			throw new MissingComponentException("Share Link Choice view object is missing a component that implements IAnimatedView on: " + this);

		timeToTidayView = timeToTidayViewObject.GetComponent<IAnimatedView>();
		if (timeToTidayView == null)
			throw new MissingComponentException("Time to Tidy view object is missing a component that implements IAnimatedView on: " + this);

		uploadingView = uploadingViewObject.GetComponent<IAnimatedView>();
		if (uploadingView == null)
			throw new MissingComponentException("Uploading view object is missing a component that implements IAnimatedView on: " + this);

		uploadFailedView = uploadFailedObject.GetComponent<IAnimatedView>();
		if (uploadFailedView == null)
			throw new MissingComponentException("Upload Failed view object is missing a component that implements IAnimatedView on: " + this);

		authConfirmView = authConfirmObject.GetComponent<IAnimatedView>();
		if (authConfirmView == null)
			throw new MissingComponentException("Auth Confirm view object is missing a component that implements IAnimatedView on: " + this);

		aboutView = aboutViewObject.GetComponent<IAnimatedView>();
		if (aboutView == null)
			throw new MissingComponentException("About view object is missing a component that implements IAnimatedView on: " + this);

		accountProhibitedView = accountProhibitedObject.GetComponent<IAnimatedView>();
		if (accountProhibitedView == null)
			throw new MissingComponentException("Account Prohibited view object is missing a component that implements IAnimatedView on: " + this);

		facebookShareBackgroundView = facebookShareBackgroundObject.GetComponent<IAnimatedView>();
		if (facebookShareBackgroundView == null)
			throw new MissingComponentException("Facebook Share Background view object is missing a component that implements IAnimatedView on: " + this);

		blankShareBackgroundView = blankShareBackgroundObject.GetComponent<IAnimatedView>();
		if (blankShareBackgroundView == null)
			throw new MissingComponentException("Blank Share Background view object is missing a component that implements IAnimatedView on: " + this);

		contactsPermissionErrorView = contactsPermissionErrorObject.GetComponent<IAnimatedView>();
		if (contactsPermissionErrorView == null)
			throw new MissingComponentException("Contacts Permission Error view object is missing a component that implements IAnimatedView on: " + this);

		noGyroView = noGyroObject.GetComponent<IAnimatedView>();
		if (noGyroView == null)
			throw new MissingComponentException("No Gyro view object is missing a component that implements IAnimatedView on: " + this);

		badConnectionView = badConnectionObject.GetComponent<IAnimatedView>();
		if (badConnectionView == null)
			throw new MissingComponentException("No Gyro view object is missing a component that implements IAnimatedView on: " + this);

		driveAuthFailedView = driveAuthFailedObject.GetComponent<IAnimatedView>();
		if (driveAuthFailedView == null)
			throw new MissingComponentException("Drive Auth Failed view object is missing a component that implements IAnimatedView on: " + this);

		views = new IAnimatedView[] { editView, listView, viewView, savingView, deleteView, clearView, saveCompleteView, shareLinkChoiceView, timeToTidayView,  googleView, shareChoiceView, uploadingView, uploadFailedView, authConfirmView, aboutView, accountProhibitedView, facebookShareBackgroundView, blankShareBackgroundView, contactsPermissionErrorView, noGyroView, badConnectionView, driveAuthFailedView };

		defaultView = editView;

		if (libaryController == null)
			libaryController = FindObjectOfType<LibraryController>();
	}

	void Start()
	{
		
	}

	void OnApplicationQuit()
	{

		Debug.Log("Application ending after " + Time.time + " seconds");
		googleAnalytics.LogEvent("App", "Quit", "current view: " + currentView.Name, 1);

	}

	public bool InPreview
	{
		get { return viewStack.Contains(viewView); }
	}

	public bool InLibrary
	{
		get { return viewStack.Contains(listView); }
	}

	public void Back()
	{
		if (viewStack.Count >= 2)
		{
			viewStack.Pop();
			StartCoroutine(TransitionView(currentView, viewStack.Peek(), corssfadePercentage));
		}
		else
		{
			Debug.LogWarning("Back called when the view stack did not have 2 or more items, replacing with default view");
			ShowView(defaultView, ShowMode.ReplaceTop);
		}
	}

	public void BackN(int n)
	{
		if (viewStack.Count >= 2)
		{
			for (int i = 0; i < n; i++)
			{
				if (viewStack.Count >= 2)
					viewStack.Pop();
				else
					Debug.LogWarning("BackN called when the view stack did not have 2 or more items, replacing with last view on the stack");

			}
			StartCoroutine(TransitionView(currentView, viewStack.Peek(), corssfadePercentage));
		}
		else
		{
			Debug.LogWarning("BackN called when the view stack did not have 2 or more items, replacing with default view");
			ShowView(defaultView, ShowMode.ReplaceTop);
		}
	}

	public void BackUntilPopped(IAnimatedView view)
	{
		while (viewStack.Count > 0)
		{
			if (viewStack.Pop() == view)
				break;
		}

		if (viewStack.Count > 0)
		{
			StartCoroutine(TransitionView(currentView, viewStack.Peek(), corssfadePercentage));
		}
		else
		{
			Debug.LogWarning("BackUntilPopped('" + view.Name + "') stack was empty after popping. replacing with default view");
			ShowView(defaultView, ShowMode.ReplaceTop);
		}
	}

	public void BackUntil(IAnimatedView view)
	{
		while (viewStack.Count > 0)
		{
			if (viewStack.Pop() == view)
				break;
		}

		ShowView(view, ShowMode.PushView);
	}

	public void BackUntilButPush(IAnimatedView backUntilView, IAnimatedView pushView)
	{
		while (viewStack.Count > 0)
		{
			if (viewStack.Pop() == backUntilView)
				break;
		}
		viewStack.Push(backUntilView);
		ShowView(pushView, ShowMode.PushView);
	}


	public void ShowView(IAnimatedView view, ShowMode mode = ShowMode.ReplaceTop)
	{
		if (currentView == null)
		{
			// we assume this is the first time we are showing a view, so just to be safe hide the other views...
			for (int i = 0; i < views.Length; i++)
			{
				SetViewActive(views[i], false);
			}
		}

		if (view == currentView)
			return;

		if (transitionPending)
		{
			if (Debug.isDebugBuild)
				Debug.Log("Show(view) request was queue due to pending transition: " + view.Name);

			// store to call when the pending transition is done, this over-writes with the latest request on purpose
			nextView = view;
			nextViewShowMode = mode;
			return;
		}
		else
		{
			nextView = null;
		}

		if (Debug.isDebugBuild)
			Debug.Log("Beginning view transition: " + view.Name);

		if (mode == ShowMode.ReplaceTop && viewStack.Count > 1)
		{
			viewStack.Pop();

		}

		viewStack.Push(view);

		googleAnalytics.LogScreen(view.Name);

		// Fire the view change event immediately after the view is pushed onto
		// the stack. This updates the InPreview and InLibrary properties
		// without waiting for the view to actually transition.
		if (ViewChanged != null)
		{
			ViewChanged();
		}

		StartCoroutine(TransitionView(currentView, view, corssfadePercentage));
	}

	#region Animation

	private void SetViewActive(IView view, bool active)
	{
		if (view == null)
			return;

		for (int i = 0; i < view.ManageActive.Length; i++)
		{
			view.ManageActive[i].SetActive(active);
		}
	}

	private void TriggerViewAnimation(IView view, string trigger)
	{
		IAnimatedView av = view as IAnimatedView;

		if (av == null)
			return;

		if (av.Animators == null || av.Animators.Length == 0)
			return;

		if (string.IsNullOrEmpty(trigger))
		{
			Debug.LogWarning("Missing trigger name on view: " + view.Name);
			return;
		}

		for (int i = 0; i < av.Animators.Length; i++)
		{
			av.Animators[i].SetTrigger(trigger);
		}
	}

	IEnumerator TransitionView(IAnimatedView oldView, IAnimatedView newView, float crossfadePercent = 0.0f)
	{
		// crossfade delay
		transitionPending = true;
		float startTime = Time.time;
		float newStartTime = startTime;
		float newEndTime = startTime;
		bool oldViewDone = true;
		bool newViewStarted = false;

		if (oldView != null)
		{
			newStartTime = startTime + (1 - crossfadePercent) * oldView.OutTime;
			TriggerViewAnimation(oldView, oldView.OutTrigger);
			oldViewDone = false;
			oldView.LeavingView(newView);
		}

		if (newView != null)
		{
			newEndTime = newStartTime + newView.InTime;
		}

		while (true)
		{
			float ellapsed = Time.time - startTime;

			if (!oldViewDone)
			{
				float p = Mathf.Clamp01(ellapsed / oldView.OutTime);

				oldView.LeavingPercent(p);

				if (p == 1.0f)
				{
					oldView.OutOfView(newView);
					SetViewActive(oldView, false);
					oldViewDone = true;
				}
			}

			if (!newViewStarted && Time.time >= newStartTime)
			{
				SetViewActive(newView, true);
				newView.EnteringView(oldView);
				TriggerViewAnimation(newView, newView.InTrigger);
				newViewStarted = true;
				currentView = newView;
			}

			if (newViewStarted)
			{
				float p = Mathf.Clamp01((Time.time - newStartTime) / newView.InTime);
				newView.EnteringPercent(p);
				if (p == 1.0f)
				{
					newView.InView(oldView);
					break;
				}
			}

			yield return null;
		}

		transitionPending = false;

		DumpViewStack();

		if (nextView != null)
		{
			// special case for Back show mode for now...
			if (nextViewShowMode == ShowMode.Back)
				Back();
			else
				ShowView(nextView, nextViewShowMode);
		}
	}

	#endregion

	#region Time To Tidy Error Handlers
	public void AboutView_BackButtonPressed(){
		Back ();
	}

	public void AboutView_OpenSourcePressed(){

	}

	public void AboutView_PrivacyPolicyPressed(){

		Application.OpenURL ("https://www.google.com/policies/privacy/");
	}

	public void AboutView_TermsOfAgreementPressed(){
		Application.OpenURL ("https://www.google.com/policies/terms/");
	}

	#endregion

	#region Time To Tidy Error Handlers
	public void TimeToTidyView_showGalleryPressed()
	{
		BackUntilButPush(editView, listView);
		libaryController.ShowMenu();
		googleAnalytics.LogEvent("UI Interaction", "Button Press", "Time To Tidy Library", 1);
	}
	#endregion

	#region Edit Button Event Handlers

	public void EditLibraryPressed()
	{
		if (Debug.isDebugBuild)
			Debug.Log("Edit | Library pressed");
		googleAnalytics.LogEvent("UI Interaction", "Button Press", "Edit Library", 1);
		ShowView(listView, ShowMode.PushView);
	}


	public void EditSavePressed()
	{
		if (Debug.isDebugBuild)
			Debug.Log("Edit | Save pressed");

		googleAnalytics.LogEvent("UI Interaction", "Button Press", "Edit Save", 1);

		if (sprayCam.GetSprayCount () < _SPRAY_LIMIT) {
			ShowView(savingView, ShowMode.PushView);
			StartCoroutine (SaveCoroutine ());
		} else {
			ShowView(timeToTidayView, ShowMode.PushView);
		}
	}

	public void EditClearPressed()
	{
		if (Debug.isDebugBuild)
			Debug.Log("Edit | Clear pressed");
		googleAnalytics.LogEvent("UI Interaction", "Button Press", "Edit Clear", 1);
		ShowView(clearView, ShowMode.PushView);
	}

	public void EditBrushSizePressed()
	{
		if (Debug.isDebugBuild)
			Debug.Log("Edit | Brush Size pressed");
		googleAnalytics.LogEvent("UI Interaction", "Button Press", "Edit Brush Size", 1);
	}

	public void EditCameraFacingPressed()
	{
		if (Debug.isDebugBuild)
			Debug.Log("Edit | Camera Facing pressed");
		googleAnalytics.LogEvent("UI Interaction", "Button Press", "Edit Camera Facing", 1);
	}

	#endregion

	#region Preview Button Handlers

	public void PreviewDeletePressed()
	{
		if (Debug.isDebugBuild)
			Debug.Log("Preview | Delete pressed");

		ShowView(deleteView, ShowMode.PushView);
	}

	public void PreviewEditPressed()
	{
		if (Debug.isDebugBuild)
			Debug.Log("Preview | Edit pressed");

		googleAnalytics.LogEvent("UI Interaction", "Button Press", "Preview Edit", 1);
		sprayCam.EditSpray(sprayCam.CurrentSpray);

		BackUntil(editView);
	}

	public void PreviewSharePressed()
	{
		if (Debug.isDebugBuild)
			Debug.Log("Preview | Share pressed");

		googleAnalytics.LogEvent("UI Interaction", "Button Press", "Preview Share", 1);
		ShowShareChoice(sprayCam.CurrentSpray);
	}

	public void PreviewBackPressed()
	{
		if (Debug.isDebugBuild)
			Debug.Log("Preview | Back pressed");

		googleAnalytics.LogEvent("UI Interaction", "Button Press", "Preview Back", 1);
		sprayCam.RestoreWipTexture();

		Back();
	}

	#endregion

	#region Library Button Handlers

	public void LibraryBackPressed()
	{
		if (Debug.isDebugBuild)
			Debug.Log("Library | Back pressed");

		googleAnalytics.LogEvent("UI Interaction", "Button Press", "Library Back", 1);
		Back();
	}

	public void LibrarySettingsPressed()
	{
		if (Debug.isDebugBuild)
			Debug.Log("Library | Settings pressed");

		googleAnalytics.LogEvent("UI Interaction", "Button Press", "Library Settings", 1);
		ShowView(aboutView, ShowMode.PushView);
	}

	#endregion

	#region Delete View Handlers

	public void DeleteView_Cancel()
	{
		if (Debug.isDebugBuild)
			Debug.Log("Delete | Cancel pressed");

		googleAnalytics.LogEvent("UI Interaction", "Button Press", "Delete Cancel", 1);
		Back();
	}

	public void DeleteView_Delete()
	{
		if (Debug.isDebugBuild)
			Debug.Log("Delete | DELETE pressed");

		googleAnalytics.LogEvent("UI Interaction", "Button Press", "Delete Delete", 1);
		sprayCam.DeleteSpray(sprayCam.CurrentSpray);

		if (InPreview)
		{
			sprayCam.RestoreWipTexture();

			// we deleted the spray we were previewing so we need to pop-off preview and end up back in library
			BackUntilPopped(viewView);
		}
		else
		{
			Back();
		}
	}

	#endregion

	#region Clear View Handlers

	public void ClearView_Cancel()
	{
		if (Debug.isDebugBuild)
			Debug.Log("Clear | Cancel pressed");
		googleAnalytics.LogEvent("UI Interaction", "Button Press", "Clear Cancel", 1);
		Back();
	}

	public void ClearView_Clear()
	{
		if (Debug.isDebugBuild)
			Debug.Log("Clear | Clear pressed");

		googleAnalytics.LogEvent("UI Interaction", "Button Press", "Clear Clear", 1);
		sprayCam.ClearWorkInProgress();

		Back();
	}

	#endregion

	#region Save Complete View Handlers

	public void SaveCompleteView_MakeAnother()
	{
		if (Debug.isDebugBuild)
			Debug.Log("Share | Make Another pressed");

		googleAnalytics.LogEvent("UI Interaction", "Button Press", "Save Complete Make Another", 1);
		sprayCam.ClearWorkInProgress();
		BackUntil(editView);
	}

	public void SaveCompleteView_KeepSpraying()
	{
		if (Debug.isDebugBuild)
			Debug.Log("Share | Make Another pressed");

		googleAnalytics.LogEvent("UI Interaction", "Button Press", "Save Complete Keep Spraying", 1);
		//		sprayCam.ClearWorkInProgress();
		BackUntil(editView);
	}

	public void SaveCompleteView_Share()
	{
		if (Debug.isDebugBuild)
			Debug.Log("Share | Share Image");

		googleAnalytics.LogEvent("UI Interaction", "Button Press", "Save Complete Share", 1);
		sprayCam.ShareSprayAsImage(sprayCam.CurrentSpray);
		sprayCam.ClearWorkInProgress();
	}


	public void SaveCompleteView_ShareLink()
	{
		if (Debug.isDebugBuild)
			Debug.Log("Share | Share pressed");

		googleAnalytics.LogEvent("UI Interaction", "Button Press", "Save Complete Share Link", 1);
		LinkShareOrAuthConfirm(sprayCam.CurrentSpray);
	}

	#endregion

	#region Share Choice Handlers

	public void ShareChoice_ShareImageClicked()
	{
		if (Debug.isDebugBuild)
			Debug.Log("Share Choice | Share Image");

		googleAnalytics.LogEvent("UI Interaction", "Button Press", "Share Choice Share Image", 1);
		sprayCam.ShareSprayAsImage(shareChoiceSpray != null ? shareChoiceSpray : sprayCam.CurrentSpray);
		shareChoiceSpray = null;
	}

	public void ShareChoice_ShareLinkClicked()
	{
		if (Debug.isDebugBuild)
			Debug.Log("Share Choice | Share Link");

		googleAnalytics.LogEvent("UI Interaction", "Button Press", "Share Choice Share Link", 1);
		LinkShareOrAuthConfirm(shareChoiceSpray != null ? shareChoiceSpray : sprayCam.CurrentSpray);
	}
	public void ShareChoice_CancelClicked()
	{
		if (Debug.isDebugBuild)
			Debug.Log("Share Choice | Cancel");

		googleAnalytics.LogEvent("UI Interaction", "Button Press", "Share Choice Cancel", 1);
		shareChoiceSpray = null;
		Back();
	}

	public void ShareLinkChoice_copyLinkClicked(){
		InputField shareLink = shareLinkChoiceObject.transform.Find("InputField").GetComponent<InputField>();
		string link = shareLink.text;

		GUIUtility.systemCopyBuffer = link;

		googleAnalytics.LogEvent("UI Interaction", "Button Press", "Share Link Copy To Clipboard", 1);
	}

	public void ShareLinkChoice_openShareClicked(){
		InputField shareLink = shareLinkChoiceObject.transform.Find("InputField").GetComponent<InputField>();
		string link = shareLink.text;
		sprayCam.ShareSprayAsString (link);
		googleAnalytics.LogEvent("UI Interaction", "Button Press", "Share Link Open Native Share", 1);
	}

	public void ShareLinkChoice_openWebClicked(){
		InputField shareLink = shareLinkChoiceObject.transform.Find("InputField").GetComponent<InputField>();
		string link = shareLink.text;
		Application.OpenURL(link);

		googleAnalytics.LogEvent("UI Interaction", "Button Press", "Share Link Open Website", 1);
	}

	public void ShareLinkChoice_CancelClicked()
	{
		if (Debug.isDebugBuild)
			Debug.Log("Share Choice | Cancel");

		googleAnalytics.LogEvent("UI Interaction", "Button Press", "Share Link Choice Cancel", 1);
		shareChoiceSpray = null;
		Back();
	}

	#endregion

	#region No Camera View Handlers

	public void NoCameraView_GoToSettings()
	{
		// TODO implement settings link
	}

	#endregion

	#region Upload Failed View

	public void UploadFailed_TryAgain()
	{
		if (Debug.isDebugBuild)
			Debug.Log("Upload Failed | Try Again pressed");

		googleAnalytics.LogEvent("UI Interaction", "Button Press", "Upload Failed Try Again", 1);
		LinkShareOrAuthConfirm(shareChoiceSpray != null ? shareChoiceSpray : sprayCam.CurrentSpray);
	}

	public void UploadFailed_Cancel()
	{
		if (Debug.isDebugBuild)
			Debug.Log("Upload Failed | Close pressed");

		googleAnalytics.LogEvent("UI Interaction", "Button Press", "Upload Failed Close", 1);
		Back();
	}

	#endregion

	#region Account Prohibited View

	public void AccountProhibitedView_SwitchAccounts()
	{
		if (Debug.isDebugBuild)
			Debug.Log("Account Prohibited | Switch Accounts");
		googleAnalytics.LogEvent("UI Interaction", "Button Press", "Account Prohibited Switch Accounts", 1);

		// the account automatically gets cleared on this type of failure so we just need to try again
		StartCoroutine(sprayCam.ShareSprayAsLink(sprayCam.CurrentSpray));
	}

	public void AccountProhibitedView_Back()
	{
		if (Debug.isDebugBuild)
			Debug.Log("Account Prohibited | Back");

		googleAnalytics.LogEvent("UI Interaction", "Button Press", "Account Prohibited Back", 1);
		Back();
	}

	#endregion

	private void LinkShareOrAuthConfirm(ISpray spray)
	{
		if (!sprayCam.forceAuthConfirm && PlayerPrefs.GetInt("GoogleAuthConfirmed") == 1)
		{
			Debug.Log("LinkShareOrAuthConfirm() PlayerPrefs == " + PlayerPrefs.GetInt("GoogleAuthConfirmed"));
			shareChoiceSpray = spray;
			StartCoroutine(sprayCam.ShareSprayAsLink(spray)); // let ShareSprayAsLink handle the view changes
		}
		else
		{
			Debug.Log("LinkShareOrAuthConfirm() No account access!");
			// we need to warning user about google account access
			ShowView(authConfirmView, ShowMode.PushView);
		}
	}

	#region Auth Confirm View

	public void AuthConfirm_Continue()
	{
		if (Debug.isDebugBuild)
			Debug.Log("Auth Confirm | Continue pressed");

		googleAnalytics.LogEvent("UI Interaction", "Button Press", "Auth Confirm Continue", 1);
		PlayerPrefs.SetInt("GoogleAuthConfirmed", 1);
		StartCoroutine(sprayCam.ShareSprayAsLink(shareChoiceSpray != null ? shareChoiceSpray : sprayCam.CurrentSpray));
	}

	public void AuthConfirm_Close()
	{
		if (Debug.isDebugBuild)
			Debug.Log("Auth Confirm | Close pressed");

		googleAnalytics.LogEvent("UI Interaction", "Button Press", "Auth Confirm Close", 1);
		Back();
	}

	#endregion

	#region Contact Permission Error Dialog

	public void ContactPermissionError_TryAgain()
	{
		StartCoroutine(sprayCam.ShareSprayAsLink(shareChoiceSpray != null ? shareChoiceSpray : sprayCam.CurrentSpray)); // let ShareSprayAsLink handle the view changes
	}

	public void ContactPermissionError_Back()
	{
		Back();
	}

	#endregion

	#region Public Interface to View Changes

	public void ShowPreview()
	{
		ShowView(viewView, ShowMode.PushView);
	}

	private ISpray shareChoiceSpray = null;

	public void ShowShareChoice(ISpray spray)
	{
		// HACK: lame way to store state so we know which spray to share...
		shareChoiceSpray = spray;
		ShowView(shareChoiceView, ShowMode.PushView);
	}

	public void ShowShareLinkChoice(String link)
	{
		// HACK: lame way to store state so we know which spray to share...

		InputField shareLink = shareLinkChoiceObject.transform.Find("InputField").GetComponent<InputField>();
		shareLink.text = link;
		ShowView(shareLinkChoiceView, ShowMode.ReplaceTop);
	}

	public void ShowUploadFailed(bool replace)
	{
		ShowView(uploadFailedView, replace ? ShowMode.ReplaceTop : ShowMode.PushView);
	}

	public void ShowBadConnection(bool replace)
	{
		ShowView(badConnectionView, replace ? ShowMode.ReplaceTop : ShowMode.PushView);
	}

	public void ShowDriveAuthFailed(bool replace)
	{
		ShowView(driveAuthFailedView, replace ? ShowMode.ReplaceTop : ShowMode.PushView);
	}

	public void ShowAccountProhibited()
	{
		ShowView(accountProhibitedView, ShowMode.ReplaceTop);
	}

	public void ShowFacebookShareBackground()
	{
		ShowViewImmediate(facebookShareBackgroundView);
	}

	public void ShowBlankShareBackground()
	{
		ShowViewImmediate(blankShareBackgroundView);
	}
	public void ShowUploadingView()
	{
		ShowView(uploadingView, ShowMode.ReplaceTop);
	}

	public void ShowNoGyro()
	{
		defaultView = noGyroView;
		ShowView(noGyroView);
	}

	private void ShowViewImmediate(IAnimatedView view)
	{
		// this is a SUPER special case where we want to pop the view on immediately with no animation
		currentView = viewStack.Peek(); // this should be the share choice dialog
		currentView.LeavingPercent(1.0f);
		currentView.OutOfView(view);
		SetViewActive(currentView, false);
		viewStack.Pop(); // pop off the share choice dialog

		currentView = viewStack.Peek();
		viewStack.Push(view);
		SetViewActive(view, true);
		view.EnteringPercent(1.0f);
		view.InView(currentView);
		currentView = view;
	}

	public void ShowContactPermissionErrorView()
	{
		ShowView(contactsPermissionErrorView, ShowMode.PushView); // replace uploading view
	}

	public void ShowDefaultView(bool clear)
	{
		if (clear)
			viewStack.Clear();

		ShowView(defaultView);
	}

	#endregion

	IEnumerator SaveCoroutine()
	{
		if (Debug.isDebugBuild)
			Debug.Log("Starting the saving processing");

		// wait for the animation in before triggering the actual save code which will hang...ugh
		// tried to do this on thread, but all texture work and unity api stuff needs to be on main thread
		// can probably get at least the upload working in a co-routine but need to refactor a bit to do that
		yield return new WaitForSeconds(savingView.InTime);
		// wait 1 more frame
		yield return null;


		sprayCam.SaveWorkInProgress ();

		if (Debug.isDebugBuild) Debug.Log ("Save complete");
		// wait 1 more frame
		yield return null;

		ShowView (saveCompleteView, ShowMode.ReplaceTop);


	}

	public void DumpViewStack()
	{
		if (Debug.isDebugBuild)
		{
			if (viewStack.Count > 0)
			{
				if (debugViewStack)
				{
					StringBuilder sb = new StringBuilder();
					int i = 0;
					foreach (var v in viewStack.ToArray())
					{
						sb.AppendFormat("{0} {1}\n", viewStack.Count - i, v.Name);
						i++;
					}
					Debug.Log(sb.ToString());
				}
				else
					Debug.Log(viewStack.Peek().Name + " " + viewStack.Count);
			}
			else
				Debug.LogWarning("View stack empty");
		}
	}

	void Update()
	{
		// Back button behavior
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			if (currentView == saveCompleteView)
				PreviewEditPressed ();
			else if (currentView == clearView)
				ClearView_Cancel ();
			else if (currentView == viewView)
				PreviewBackPressed ();
			else if (currentView == listView)
				LibraryBackPressed ();
			else if (currentView == deleteView)
				DeleteView_Cancel ();
			else if (currentView == shareChoiceView)
				ShareChoice_CancelClicked ();
			else if (currentView == shareLinkChoiceView)
				ShareLinkChoice_CancelClicked ();
			else if (currentView == uploadFailedView)
				UploadFailed_Cancel ();
			else if (currentView == authConfirmView)
				AuthConfirm_Close ();
			else if (currentView == aboutView)
				AboutView_BackButtonPressed ();
			else if (currentView == accountProhibitedView)
				AccountProhibitedView_Back ();
			else if (currentView == contactsPermissionErrorView)
				ContactPermissionError_Back ();
			else if (currentView == editView) {
				if (Application.platform == RuntimePlatform.Android) {
					AndroidJavaObject activity = new AndroidJavaClass ("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject> ("currentActivity");
					activity.Call<bool> ("moveTaskToBack", true);
				} else {
					Application.Quit ();
				}
			
			}
			else {
				Debug.Log ("No back action for view " + currentView.Name);
			}
		}
	}

	void OnDestroy()
	{

	}

	public void OnActivityResult(int requestCode, int resultCode, string intentAsString)
	{
		// called from Java-land, currently only used to know when the share dialog is done...
		switch (requestCode)
		{
		case NativeShare.SHARE_IMAGE_REQUEST_CODE:
		case NativeShare.SHARE_LINK_REQUEST_CODE:
			Back();
			break;
		default:
			// no-op
			break;
		}

	}
}
