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


public enum PhoneOrientation
{
	PortraitUp,
	PortraitDown,
	LandscapeUp,
	LandscapeDown,
}

public class UpTracker : MonoBehaviour
{
	public Transform cameraTransform;
	public float dotRequiredToSwitch = 0.6f;
	public float rotationTime = 0.5f;
	public AnimationCurve rotateCurve;

	public RectTransform[] managedTransforms;
	public PhoneOrientation currentOrientation;
	public int currentIndex;

	private PhoneOrientation[] allOrientations = new PhoneOrientation[] {
		PhoneOrientation.PortraitUp,
		PhoneOrientation.PortraitDown,
		PhoneOrientation.LandscapeUp,
		PhoneOrientation.LandscapeDown,
	};

	private bool[] checkUp = new bool[] {
		false,
		false,
		true,
		true,
	};

	private Vector3[] orientationUpVectors = new Vector3[] {
		Vector3.up,
		Vector3.down,
		Vector3.up,
		Vector3.down,
	};

	private float[] zRotations = new float[] {
		90.0f,
		270.0f,
		0.0f,
		180.0f,
	};

	private Quaternion[] rotations = new Quaternion[] {
		Quaternion.Euler(0, 0, 90.0f),
		Quaternion.Euler(0, 0, 270.0f),
		Quaternion.Euler(0, 0, 0.0f),
		Quaternion.Euler(0, 0, 180.0f),
	};

	// made this public for debugging purposes
	public float[] dots = new float[] { 0, 0, 0, 0 };

	// rotation animation tracking
	private float rotateStartTime = 0.0f;
	private Quaternion startRotation;
	private Quaternion targetRotation;
	private bool rotationHappening = false;
	private Transform head;
	private Transform cardboard;

	int ClosestOrientation(Vector3 upVector, Vector3 leftVector)
	{
		for (int i=0; i < allOrientations.Length; i++)
		{
			if (checkUp[i])
				dots[i] = Vector3.Dot(orientationUpVectors[i], upVector);
			else
				dots[i] = Vector3.Dot(orientationUpVectors[i], leftVector);
		}

		// find min
		float min = float.MaxValue;
		int minIndex = -1;
		for (int i = 0; i < allOrientations.Length; i++)
		{
			float distanceToOne = 1 - dots[i];
			if (distanceToOne < min)
			{
				min = distanceToOne;
				minIndex = i;
			}
		}

		return minIndex;
	}

	void Start()
	{
		if (cameraTransform == null)
			cameraTransform = this.transform;

		head = cameraTransform.parent;
		cardboard = head.parent;

		UpdateOrientation();
	}

	
	IEnumerator RotateTransforms()
	{
		Debug.Log("Starting ui rotation co-routine");
		rotateStartTime = Time.time;
		rotationHappening = true;

		while (true)
		{
			float ellapsed = Time.time - rotateStartTime;
			float p = Mathf.Clamp01(ellapsed / rotationTime);
			float t = rotateCurve.Evaluate(p);

			Quaternion q = Quaternion.SlerpUnclamped(startRotation, targetRotation, t);

			for (int i = 0; i < managedTransforms.Length; i++)
			{
				managedTransforms[i].localRotation = q; 
			}

			if (p == 1.0f)
				break;

			yield return null;
		}

		rotationHappening = false;
		Debug.Log("Starting ui rotation complete");
	}

	void UpdateOrientation()
	{
		int newIndex = ClosestOrientation(cameraTransform.up, -cameraTransform.right);
		PhoneOrientation o = allOrientations[newIndex];
		float dot = dots[newIndex];
		if (o != currentOrientation && dot > dotRequiredToSwitch)
		{
			currentOrientation = o;
			currentIndex = newIndex;
			Debug.Log("New orientation detected: " + currentOrientation);

			if (managedTransforms.Length > 0)
			{
				rotateStartTime = Time.time;
				startRotation = managedTransforms[0].localRotation;
				targetRotation = rotations[newIndex];

				if (rotationHappening)
				{
					// we updated the currently running co-routine, no need to start a new one
				}
				else
				{
					StartCoroutine(RotateTransforms());
				}
			}
		}
		
	}

	// Update is called once per frame
	void Update()
	{
		UpdateOrientation();
	}
}
