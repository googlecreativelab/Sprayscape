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
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class UpdateCameraFacing : MonoBehaviour
{
	public SprayCam sprayCam;
	public RawImage image;
	public RawImage largeImage;
	public RectTransform flipTransform;
	public RectTransform largeFlipTransform;
	public Texture2D frontFacing;
	public Texture2D backFacing;
	public Texture2D frontFacingLarge;
	public Texture2D backFacingLarge;

	public float flipTime = 0.15f;
	public bool animateBeforeToggle = true;

	private Dictionary<CameraFacing, Texture2D> textureMap = new Dictionary<CameraFacing, Texture2D>();
	private Dictionary<CameraFacing, Texture2D> textureMapLarge = new Dictionary<CameraFacing, Texture2D>();
	private bool flipping = false;

	protected float alpha = 1.0f;
	void Awake()
	{
		if (sprayCam == null)
			sprayCam = FindObjectOfType<SprayCam>();

		if (image == null)
			image = GetComponent<RawImage>();

		if (flipTransform == null)
			flipTransform = GetComponent<RectTransform>();

		textureMap[CameraFacing.Front] = frontFacing;
		textureMap[CameraFacing.Back] = backFacing;
		textureMapLarge[CameraFacing.Front] = frontFacingLarge;
		textureMapLarge[CameraFacing.Back] = backFacingLarge;
	}



	void OnEnable()
	{
		alpha = 1.0f;
		CanvasRenderer alphaTrans = largeFlipTransform.gameObject.GetComponent<CanvasRenderer> ();
		alphaTrans.SetAlpha (alpha);
		sprayCam.CameraFacingChanged += UpdateCameraFacingImage;
		UpdateCameraFacingImage(sprayCam.CameraFacing);
	}

	void OnDisable()
	{
		sprayCam.CameraFacingChanged -= UpdateCameraFacingImage;
	}

	void Update(){
		if (CameraFlipTimer > 0) {
			CameraFlipTimer -= Time.deltaTime;
		}
	}


	void UpdateCameraFacingImage(CameraFacing cameraFacing)
	{
		image.texture = textureMap[cameraFacing];
		largeImage.texture = textureMapLarge[cameraFacing];
	}

	private const float _CAMERA_DEBOUNCE_TIME = 0.2f;
	private float CameraFlipTimer = 0;

	public void ToggleCameraFacing()
	{
		if (CameraFlipTimer <= 0) {
			CameraFlipTimer = _CAMERA_DEBOUNCE_TIME;
			// here we decide if we want to flip before or after...
			if (!animateBeforeToggle)
				sprayCam.ToggleCameraFacing ();

			StartCoroutine (FlipIcon ());
		}
	}

	IEnumerator FlipIcon()
	{
		largeFlipTransform.gameObject.SetActive(true);

		CanvasRenderer alphaTrans = largeFlipTransform.gameObject.GetComponent<CanvasRenderer> ();


		flipping = true;
		float startTime = Time.time;

		while (true)
		{
			float ellapsed = Time.time - startTime;
			float p = Mathf.Clamp01(ellapsed / flipTime);
			float a = Mathf.Lerp(0, 90.0f, p);
			flipTransform.localEulerAngles = new Vector3(0, a, 0);
			largeFlipTransform.localEulerAngles = new Vector3(0, a, 0);
			if (p == 1.0)
				break;

			yield return null;
		}

		// swap texture now
		startTime = Time.time;
		image.texture = image.texture == frontFacing ? backFacing : frontFacing;
		largeImage.texture = largeImage.texture == frontFacingLarge ? backFacingLarge : frontFacingLarge;


		// rotate back
		while (true)
		{
			float ellapsed = Time.time - startTime;
			float p = Mathf.Clamp01(ellapsed / flipTime);
			float a = Mathf.Lerp(90.0f, 0, p);
			flipTransform.localEulerAngles = new Vector3(0, a, 0);
			largeFlipTransform.localEulerAngles = new Vector3(0, a, 0);



			if (p == 1.0)
				break;

			yield return null;
		}

		// wait a few frames before triggering the webcam flip so the screen is updated with latest rotation
		yield return null;
		yield return null;
		yield return null;

		if (animateBeforeToggle)
			sprayCam.ToggleCameraFacing();

		alpha = 1.0f;	

		while (true)
		{
			alpha = alpha - 0.05f;
			alphaTrans.SetAlpha (alpha);

			if(alpha <= 0) 
				break;

			yield return true;
		}

		// TODO: fade this out...
		largeFlipTransform.gameObject.SetActive(false);

		flipping = false;
	}
}
