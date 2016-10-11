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

public class HelperWireframe : MonoBehaviour
{
	#region Inspector

	public SprayCam sprayCam = null;

	[Tooltip("The wireframe material to fade.")]
	public Material wireframeMaterial;

	[Tooltip("The curve to use when fading the wireframe opacity.")]
	public AnimationCurve fadeCurve;

	#endregion

	#region Private

	private Renderer meshRenderer;

	// A reference to the current fade coroutine so we can cancel it.
	private IEnumerator fadeCoroutine = null;
	
	// Track the visible-ness of the wireframe so we can match it with the
	// blank-ness of the spraycam.
	private bool visible = false;

	#endregion

	#region Lifecycle

	void Awake()
	{
		if (sprayCam == null)
		{
			sprayCam = FindObjectOfType<SprayCam>();
		}

		meshRenderer = GetComponent<MeshRenderer>();
	}

	void Start()
	{
		if (sprayCam == null)
		{
			gameObject.SetActive(false);
		}
		else
		{
			Show();
		}
	}

	void Update()
	{
		if (sprayCam.Blank && !visible)
		{
			Show();
		}
		else if (!sprayCam.Blank && visible)
		{
			Hide();
		}
	}

	#endregion

	private void Show()
	{
		visible = true;

		CancelFadeCoroutine();

		fadeCoroutine = FadeCoroutine();
		StartCoroutine(fadeCoroutine);
	}

	private void Hide()
	{
		visible = false;

		CancelFadeCoroutine();

		// Instead of using the coroutine here, just stop rendering. Fading the
		// wireframe looks kind of weird when you're drawing to the spray texture.
		meshRenderer.enabled = false;
	}

	private IEnumerator FadeCoroutine()
	{
		meshRenderer.enabled = true;

		float duration = fadeCurve.Duration();
		float elapsed  = 0f;
		
		while (elapsed < duration)
		{
			float evaluated = fadeCurve.Evaluate(elapsed / duration);
			wireframeMaterial.SetFloat("_LineOpacity", evaluated);

			elapsed += Time.deltaTime;

			yield return null;
		}
	}

	private void CancelFadeCoroutine()
	{
		if (fadeCoroutine != null)
		{
			StopCoroutine(fadeCoroutine);
			fadeCoroutine = null;
		}
	}
}
