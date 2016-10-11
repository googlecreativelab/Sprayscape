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

public class CameraAcquirer : MonoBehaviour
{
	#region Private

	WebCamTexture wct = null;

	bool useFront = false;
	bool cameraAuthorized = false;
	float timeSinceLastAuthorizationCheck = -1000;
	const float reauthorizeTimeout = 4;

	float timeSinceLastDeviceCheck = -1000;
	const float deviceCheckTimeout = 1;

	#endregion

	#region Properties

	public WebCamTexture WCT { get { return wct; } }
	public bool UseFront { get { return useFront; } }

	#endregion
	
	void Start()
	{
		AuthorizeCamera();

		if (cameraAuthorized)
		{
			InitDevice();
		}
	}

	void OnApplicationFocus(bool focusStatus)
	{
		cameraAuthorized = false;

		AuthorizeCamera();

		if (cameraAuthorized)
		{
			InitDevice();
		}
	}

	void OnApplicationPause(bool pauseStatus)
	{
		cameraAuthorized = false;

		DestroyDevice();
	}

	void AuthorizeCamera()
	{
		if (Application.HasUserAuthorization(UserAuthorization.WebCam))
		{
			cameraAuthorized = true;
		}
		else
		{
			Application.RequestUserAuthorization(UserAuthorization.WebCam);
			timeSinceLastAuthorizationCheck = Time.time;
		}
	}

	void Update()
	{
		if (cameraAuthorized)
		{
			if (wct == null)
			{
				if (Time.time - timeSinceLastDeviceCheck > deviceCheckTimeout)
				{
					timeSinceLastDeviceCheck = Time.time;
					//Debug.Log("Camera not found - Rechecking for camera");
					InitDevice();

				}
			}
		}
		else
		{
			if (Time.time - timeSinceLastAuthorizationCheck > reauthorizeTimeout)
			{
				AuthorizeCamera();
			}
		}
	}

	void InitDevice()
	{
		DestroyDevice();

		WebCamDevice[] devices = WebCamTexture.devices;

		if (devices != null && devices.Length > 0)
		{
			// Find the first camera with a matching orientation.
			for (int i = 0; i < devices.Length; i++)
			{
				if (devices[i].isFrontFacing == useFront)
				{
					wct = new WebCamTexture(devices[i].name, 1280, 720);

					if (Debug.isDebugBuild)
					{
						Debug.LogFormat("Using device [ {0} : {1} ]", i, devices[i].name);
					}

					break;
				}
			}

		}
		else if (Debug.isDebugBuild)
		{
			Debug.LogError("Device list is empty");
		}
	}

	void DestroyDevice()
	{
		if (wct != null)
		{
			wct.Stop();
			Destroy(wct);
		}
	}

	public void SwapCameraDirection()
	{
		SetCameraDirection(!useFront);
	}

	public void SetCameraDirection(bool useFront)
	{
		if (this.useFront != useFront)
		{
			this.useFront = useFront;
			InitDevice();
		}
	}
}
