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
using System.IO;
using NatCamU;
using System.Collections;

public class CameraCapture : MonoBehaviour
{



	#region Inspector

	public Camera mainCamera;

	[Tooltip("The material used to render the work-in-progress spray.")]
	public Material renderingMaterial;

	[Tooltip("The material used to composite the camera image onto the work-in-progress spray.")]
	public Material compositingMaterial;

	[Tooltip("The alpha multiplier used when compositing sprays. A lower value will take longer to burn in the image.")]
	public float compositingAlpha = 0.1f;

	public FocusMode deviceCameraFocusMode = FocusMode.AutoFocus;
	public ExposureMode deviceCameraExposureMode = ExposureMode.AutoExpose;
	public FrameratePreset deviceCameraFramerate = FrameratePreset.Smooth;
	public ResolutionPreset deviceCameraResolution = ResolutionPreset.MediumResolution;

	[Tooltip("Release the NatCam state when the camera facing switches.")]
	public bool releaseOnSwitch = false;

	[Tooltip("The number of stale frames to consume before treating the camera data as valid.")]
	public int staleFramesAllowed = 3;

	#endregion

	#region Private

	private bool initialized = false;
	
	private RenderTexture sprayComposite;

	private int staleFramesReceived = 0;
	
	private Texture activeTexture;

	#endregion

	#region Properties

	public bool UseFront { get { return initialized && NatCam.ActiveCamera != null && NatCam.ActiveCamera.Facing == Facing.Front; } }
	
	public bool IsTorchEnabled { get { return initialized && NatCam.ActiveCamera != null && NatCam.ActiveCamera.TorchMode == Switch.On; } }
	public bool IsTorchSupported { get { return initialized && NatCam.ActiveCamera != null && NatCam.ActiveCamera.IsTorchSupported; } }

	#endregion
		
	#region Events

	public event Action<CameraFacing> FacingChanged;
	public event Action<bool> TorchChanged;
		
	#endregion

	#region Image Loading
	
	public void LoadImage(Texture2D texture)
	{
		RenderTexture previous = RenderTexture.active;
		RenderTexture.active = sprayComposite;

		Graphics.Blit(texture, sprayComposite);

		RenderTexture.active = previous;
	}

	#endregion

	#region Lifecycle

	void Awake()
	{
		InitTextures();
		SetBigCamSize();
		Shader.SetGlobalFloat("_CaptureAlpha", compositingAlpha);
		Shader.SetGlobalVector("_CaptureScale", new Vector2(1, 1));
	}

    IEnumerator WaitForPermissionCoroutine()
    {
        yield return null;
        var sprayCam = FindObjectOfType<SprayCam>();
        while (sprayCam.hasCameraPermission.HasValue == false || sprayCam.hasCameraPermission.Value != true)
        {
            yield return null;
        }

        InitCamera();
        ActivateCamera(CameraFacing.Back);
    }

    void Start()
	{
        StartCoroutine(WaitForPermissionCoroutine());
	}

	void Update()
	{
		Shader.SetGlobalMatrix("_VP", mainCamera.projectionMatrix * mainCamera.worldToCameraMatrix);
	}

	#endregion

	#region Buffer Management

	private void InitTextures()
	{
		sprayComposite = new RenderTexture(2048, 1024, 0);
		sprayComposite.DiscardContents();
		sprayComposite.useMipMap = true;
		
		renderingMaterial.mainTexture = sprayComposite;

		Clear();
	}
	
	public void Clear()
	{
		RenderTexture previous = RenderTexture.active;

		RenderTexture.active = sprayComposite;
		GL.Clear(false, true, Color.black);

		RenderTexture.active = previous;
	}

	#endregion

	private void InitCamera()
	{
        Debug.Log("InitCamera() called, starting natcam");
		#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
		NatCam.Initialize(NatCamInterface.NativeInterface, PreviewType.NonReadable, Switch.Off);
		#else
		NatCam.Initialize(NatCamInterface.FallbackInterface, PreviewType.NonReadable, Switch.Off);
		#endif

		initialized = true;
	}





	private void ActivateCamera(CameraFacing facing)
	{
		if (!initialized) return;
		
		// Any spray compositing that happens while the camera is initializing should spray pitch black.
		activeTexture = Texture2D.blackTexture;
		staleFramesReceived = 0;

		var device = (facing == CameraFacing.Front) ? DeviceCamera.FrontCamera : DeviceCamera.RearCamera;

		if (device != null)
		{
			// We can't change the resolution while the preview is playing.
			if (NatCam.IsPlaying) NatCam.Pause();

			NatCam.ActiveCamera = device;
			NatCam.OnPreviewStart += OnPreviewStart;
			NatCam.OnPreviewUpdate += OnPreviewUpdate;

			NatCam.ActiveCamera.SetFramerate(deviceCameraFramerate);
			NatCam.ActiveCamera.SetResolution(deviceCameraResolution);

			// The exposure and focus modes are only available on the native interface.
			if (NatCam.Interface == NatCamInterface.NativeInterface)
			{
				NatCam.ActiveCamera.FocusMode = deviceCameraFocusMode;
				NatCam.ActiveCamera.ExposureMode = deviceCameraExposureMode;
			}

			NatCam.Play();

			if (FacingChanged != null) FacingChanged(facing);
		}
		else if (Debug.isDebugBuild)
		{
			Debug.LogError("CameraCapture: no camera for this orientation");
		}
	}

	private void ReleaseCamera()
	{
		if (NatCam.ActiveCamera != null)
		{
			NatCam.Release();
		}
		
		initialized = false;
	}
	
	private void OnPreviewStart()
	{
		Debug.Log("CameraCapture: preview start");

		Vector2 screenResolution = new Vector2(Screen.width, Screen.height);
		Vector2 cameraResolution = NatCam.ActiveCamera.ActiveResolution;

		float screenAspect = screenResolution.x / screenResolution.y;
		float cameraAspect = cameraResolution.x / cameraResolution.y;

		#if !UNITY_EDITOR
		// There are some inconsistencies with the camera aspect ratio in the
		// editor and on the devices. This fix is tested on a Nexus 5X and 6.
		cameraAspect = 1 / cameraAspect;
		#endif
	
		Vector2 imageScale = new Vector2
		{
			x = (cameraAspect > screenAspect) ? screenAspect / cameraAspect : 1,
			y = (cameraAspect < screenAspect) ? screenAspect / cameraAspect : 1
		};

		Shader.SetGlobalVector("_CaptureScale", imageScale);

		NatCam.OnPreviewStart -= OnPreviewStart;
	}

	private void OnPreviewUpdate()
	{
		if (staleFramesReceived < staleFramesAllowed)
		{
			if (Debug.isDebugBuild) Debug.Log("CameraCapture: ignoring stale frame");
			
			// Increment the number of stale frames. After switching camera
			// facing, Android reports 1-2 frame updates with stale data from
			// the previous facing. This is a super hacky workaround but it
			// seems reliable. Tested on a Nexus 5X and Nexus 6
			staleFramesReceived += 1;
		}
		else
		{
			if (Debug.isDebugBuild) Debug.Log("CameraCapture: switching to camera preview");
			
			// Start spraying the camera preview now that it's initialized.
			activeTexture = NatCam.Preview;
			NatCam.OnPreviewUpdate -= OnPreviewUpdate;
		}
	}
	
	public void ToggleCameraTorch()
	{
		SetCameraTorch(!IsTorchEnabled);
	}
	
	public void SetCameraTorch(bool on)
	{
		if (NatCam.ActiveCamera == null || NatCam.ActiveCamera.IsTorchSupported == false)
		{
			return;
		}
		else if (IsTorchEnabled != on)
		{
			NatCam.ActiveCamera.TorchMode = on ? Switch.On : Switch.Off;
			if (TorchChanged != null) TorchChanged(on);
		}

	}

	public void SetCameraDirection(bool front)
	{
		// Don't make unneccessary camera switches.
		if (front == UseFront) return;
		
		if (releaseOnSwitch)
		{
			//ReleaseCamera();
			//InitCamera();
		}

		ActivateCamera(front ? CameraFacing.Front : CameraFacing.Back);
	}

	public Texture2D CopyToNewTexture()
	{
		RenderTexture previous = RenderTexture.active;
		RenderTexture.active = sprayComposite;

		Texture2D texture = new Texture2D(sprayComposite.width, sprayComposite.height);
		texture.ReadPixels(new Rect(0, 0, sprayComposite.width, sprayComposite.height), 0, 0);
		
		RenderTexture.active = previous;

		return texture;
	}

	public void SetBigCamSize()
	{
		Shader.SetGlobalFloat("_CaptureSize", 1);
	}

	public void SetMedCamSize()
	{
		Shader.SetGlobalFloat("_CaptureSize", .6f);
	}

	public void SetSmallCamSize()
	{
		Shader.SetGlobalFloat("_CaptureSize", .3f);
	}

	public bool Capture()
	{
		if (activeTexture != null)
		{
			Graphics.Blit(activeTexture, sprayComposite, compositingMaterial);
			return true;
		}
		else
		{
			return false;
		}
	}

	public int Save()
	{
		return PhotoIO.SaveToLocalStore(sprayComposite);
	}
}
