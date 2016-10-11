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
using System.Collections.Generic;
using UnityEngine.UI;

public class UpdateBrushPreview : MonoBehaviour
{
	public SprayCam sprayCam;
	public RawImage image;
	public Texture2D largeBrushPreview;
	public Texture2D mediumBrushPreview;
	public Texture2D smallBrushPreview;

	[Tooltip("The alpha value to start fading from when the brush size changes.")]
	public float fadeAlpha = 0.2f;

	[Tooltip("The duration of the fade when the brush size changes.")]
	public float fadeDuration = 0.25f;
	public bool isFadeEnabled = true;

	private Dictionary<BrushSize, Texture2D> textureMap = new Dictionary<BrushSize, Texture2D>();

	void Awake()
	{
		if (sprayCam == null)
		{
			sprayCam = FindObjectOfType<SprayCam>();
		}

		if (image == null)
		{
			image = GetComponent<RawImage>();
		}
		if (isFadeEnabled)
			image.color = new Color(1f, 1f, 1f, 0);
		else
			image.color = new Color(1f, 1f, 1f, 1f);

		textureMap[BrushSize.Big]    = largeBrushPreview;
		textureMap[BrushSize.Medium] = mediumBrushPreview;
		textureMap[BrushSize.Small]  = smallBrushPreview;
	}

	void OnEnable()
	{
		sprayCam.BrushSizeChanged += SprayCam_BrushSizeChanged;
	}

	void OnDisable()
	{
		sprayCam.BrushSizeChanged -= SprayCam_BrushSizeChanged;
	}

	void SprayCam_BrushSizeChanged(BrushSize brushSize)
	{
		image.texture = textureMap[brushSize];

		StopAllCoroutines();
		if (isFadeEnabled)
			StartCoroutine(FadeBrushPreview());
		else
			image.color = new Color(1f, 1f, 1f, 1f);
	}
	
	IEnumerator FadeBrushPreview()
	{
		float elapsed = 0f;

		while (elapsed < fadeDuration)
		{
			elapsed += Time.deltaTime;

			float alpha = Mathf.Lerp(fadeAlpha, 0f, elapsed / fadeDuration);
			image.color = new Color(1f, 1f, 1f, alpha);

			yield return null;
		}
	}
}
