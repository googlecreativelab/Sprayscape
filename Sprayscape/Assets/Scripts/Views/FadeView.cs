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

public class FadeView : MonoBehaviour, IAnimatedView
{
	public Animator[] animators = new Animator[0];
	public GameObject[] manageActive = new GameObject[0];

	public CanvasGroup[] groups;

	public float inTime;
	public float outTime;

	public AnimationCurve inCurve;
	public AnimationCurve outCurve;

	#region IAnimatedView

	public virtual string Name { get { return this.gameObject.name; } }
	public virtual Animator[] Animators { get { return animators; } }
	public virtual GameObject[] ManageActive { get { return manageActive; } }
	public virtual float InTime { get { return inTime; } }
	public virtual float OutTime { get { return outTime; } }
	public string InTrigger { get { return "in"; } }
	public string OutTrigger { get { return "out"; } }

	public virtual void EnteringView(IView previous) { }
	public virtual void EnteringPercent(float percent)
	{
		float a = inCurve.Evaluate(percent);
		SetAlphas(a);

	}
	public virtual void InView(IView previous) { }
	public virtual void LeavingView(IView newView) { }
	public virtual void LeavingPercent(float percent)
	{
		float a = outCurve.Evaluate(percent);
		SetAlphas(a);
	}
	public virtual void OutOfView(IView newView) { }

	private void SetAlphas(float alpha)
	{
		if (groups != null)
		{
			for (int i = 0; i < groups.Length; i++)
			{
				groups[i].alpha = alpha;
			}
		}
	}

	#endregion
}
