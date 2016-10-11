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

public class FadeCanvasGroup : MonoBehaviour
{
	public CanvasGroup group;
	public AnimationCurve fadeOutCurve;
	public float delay = 0.2f;
	public float fadeTime = 0.3f;
    private bool disableObject = true;

	public void Awake()
	{
		if (group == null)
			group = GetComponent<CanvasGroup>();
	}
	
	private IEnumerator FadeOut()
	{
		yield return new WaitForSeconds(delay);

		float startTime = Time.time;
		while (true)
		{
			float p = Mathf.Clamp01((Time.time - startTime) / fadeTime);
			float t = fadeOutCurve.Evaluate(p);
			group.alpha = Mathf.Lerp(0.0f, 1.0f, t);

			if (p == 1.0f)
				break;

			yield return null;
		}

        if (disableObject)
            this.gameObject.SetActive(false);


	}

	void OnEnable()
	{
		StartCoroutine(FadeOut());
	}
}
