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
using UnityEngine.UI;

public class FrameSequencePlayer : MonoBehaviour
{
	public string sequenceFormat = "SPRAYSCAPE_{0:D5}";
	public RawImage rawImage;
	public int firstFrame = 0;
	public int lastFrame = 39;
	public float playBackFPS = 15.0f;

	private int frames;
	private ResourceRequest resourceRequest;
	private Texture2D empty;
	private string[] paths;
	private bool play = false;
	
	FrameSequencePlayer()
	{
		frames = lastFrame - firstFrame + 1;

		// pre allocate all that paths to avoid run-time memory allocation
		paths = new string[frames];

		for (int i = 0; i < frames; i++)
		{
			paths[i] =  string.Format(sequenceFormat, (i + firstFrame));
		}

	}

	void UpdateTextureFromResourceRequest(RawImage image, ResourceRequest rr)
	{
		if (rr.asset != null)
		{
			var oldTexture = image.texture;
			image.texture = null;
			if (oldTexture != null && oldTexture != empty)
				Resources.UnloadAsset(oldTexture);
			oldTexture = null;
			image.texture = rr.asset as Texture;
		} // else keep old texture
	}
	
	void CleanupImage(RawImage image)
	{
		var old = image.texture;
		if (old != null && old != empty)
			Resources.UnloadAsset(old);
		image.texture = empty; // prevents a white box from showing...
	}

	IEnumerator PlayMovie()
	{
		float startTime = Time.time;
		float frameTime = 1.0f / playBackFPS;
		float frameStart, frameEllapsed;

		rawImage.enabled = true;
		rawImage.texture = empty;
		
		// load first frame
		resourceRequest = Resources.LoadAsync(paths[0], typeof(Texture));
		while (!resourceRequest.isDone)
			yield return null;
		
		// present frame
		UpdateTextureFromResourceRequest(rawImage, resourceRequest);
		startTime = Time.time;
		frameStart = Time.time;
		frameEllapsed = 0.0f;

		// Intro
		int pendingFrame = 1;
		while (pendingFrame < frames && play)
		{
			//Debug.Log("loading frame: " + pendingFrame);
			// queue up next frame
			resourceRequest = Resources.LoadAsync(paths[pendingFrame], typeof(Texture));

			while (!resourceRequest.isDone || frameEllapsed < frameTime)
			{
				yield return null;
				frameEllapsed = Time.time - frameStart;
			}
			// frame is done loading and time is up, swap them out
			UpdateTextureFromResourceRequest(rawImage, resourceRequest);
			resourceRequest = null;
			// mark the start of this new frame
			frameStart = Time.time;
			frameEllapsed = 0.0f;

			// compute next frame based on elapsed time, this will account for skipped frames
			float introEllapsed = Time.time - startTime;
			int nextFrame = (int)Mathf.Floor(introEllapsed / frameTime);
			if (nextFrame > pendingFrame + 1 && Debug.isDebugBuild)
			{
				//Debug.LogWarning("dropped frame on playback: " + pendingFrame + " -> " + nextFrame);
			}

			pendingFrame = nextFrame;
		}

		CleanUpAll();

		if (this.gameObject.activeSelf)
			this.gameObject.SetActive(false);
	}

	void CleanUpAll()
	{
		CleanupImage(rawImage);
		resourceRequest = null;
		// just to be safe, force cleanup of all unused assets...
		Resources.UnloadUnusedAssets();
	}

	public void Show()
	{
		if (this.gameObject.activeSelf == false)
		{
			// enabling this will trigger another call to Show() with activeSelf == true
			this.gameObject.SetActive(true);
		}
		else
		{
			play = true;
			StartCoroutine(PlayMovie());
		}
	}

	public void Hide()
	{
		if (play)
		{
			play = false; // this will cause the co-routine to stop the co-routine will cleanup on the way out
		}
		else
		{
			if (this.gameObject.activeSelf)
				this.gameObject.SetActive(false);
		}
	}
	
	void OnEnable()
	{
		if (empty == null)
		{
			empty = new Texture2D(1, 1, TextureFormat.ARGB32, false);
			empty.SetPixel(0, 0, new Color(1, 1, 1, 0));
			empty.Apply(false, true);
		}

		Show();
	}

	void OnDisable()
	{
		Hide();
	}
}
