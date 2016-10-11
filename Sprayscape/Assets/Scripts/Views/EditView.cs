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
using UserInterface.Controllers;

/// <summary>
/// This is just a fade view that manages the onboarding object visibility as well
/// </summary>
public class EditView : FadeView
{
	public OnboardingController onboardingController;
	public GameObject Onboarding;
	private bool showOnboarding = true;

	void Awake()
	{
		if (onboardingController == null)
			onboardingController = FindObjectOfType<OnboardingController>();
	}

	public override void EnteringView(IView previous)
	{
		if (showOnboarding)
		{
			Onboarding.SetActive(true);
		}

	}

	public override void EnteringPercent(float percent)
	{
		base.EnteringPercent(percent);
		if (onboardingController)
			onboardingController.FadePercent(percent);
	}

	public override void LeavingPercent(float percent)
	{
		base.LeavingPercent(percent);
		if (onboardingController)
			onboardingController.FadePercent(1 - percent);
	}

	public override void OutOfView(IView newView)
	{
		showOnboarding = Onboarding.activeSelf;
		// always turn onboarding off when leaving the edit view
		Onboarding.SetActive(false);
	}
}
