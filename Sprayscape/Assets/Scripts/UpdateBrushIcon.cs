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

public class UpdateBrushIcon : MonoBehaviour
{
	public SprayCam sprayCam;
	public RawImage image;
	public Texture2D bigBrush;
	public Texture2D medBrush;
	public Texture2D smallBrush;

	private Dictionary<BrushSize, Texture2D> textureMap = new Dictionary<BrushSize, Texture2D>();

	void Awake()
	{
		if (sprayCam == null)
			sprayCam = FindObjectOfType<SprayCam>();

		if (image == null)
			image = GetComponent<RawImage>();

		textureMap[BrushSize.Big] = bigBrush;
		textureMap[BrushSize.Medium] = medBrush;
		textureMap[BrushSize.Small] = smallBrush;
	}

	void OnEnable()
	{
		sprayCam.BrushSizeChanged += UpdateBrushSize;
		UpdateBrushSize(sprayCam.BrushSize);
	}

	void OnDisable()
	{
		sprayCam.BrushSizeChanged -= UpdateBrushSize;
	}

	// Update is called once per frame
	void UpdateBrushSize(BrushSize brushSize)
	{
		image.texture = textureMap[brushSize];
	}

	public void ToggleBrush()
	{
		sprayCam.ToggleBrushSize();
	}
}
