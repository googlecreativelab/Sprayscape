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

namespace UserInterface.Controllers
{
	public class OnboardingController : MonoBehaviour
	{
		public Transform head;
		public SprayCam sprayCam;
		public LayerMask layersToRaycast;
		public GameObject[] sprayTargetGameObjects;
		public GameObject[] connectorGameObjects;
		public GameObject rotationNotice;
		public RawImage tapNoticeText, tapNoticeArrow;
		public Transform onboardingTransform; // this get aligned to the camera when starting
		public float requiredRotationDegrees = 180.0f;
		public Vector3 brushRadii = new Vector3(0.5f, 0.3f, 0.1f); // large to small
		public float sphereCastOffset = 0.5f;
		public float animateInTime = 1.0f;
		public float animateOutTime = 1.0f;
		public AnimationCurve inSprayAlphaCurve;
		public AnimationCurve outSprayAlphaCurve;
		public AnimationCurve inConnectorCurve;
		public AnimationCurve outConnectorCurve;

		public GoogleAnalyticsV3 googleAnalytics;

		private bool doneWithTargets = false;
		private int currentSprayTargetIndex = 0;
		private GameObject currentSprayTarget;
		private GameObject currentConnector;
		private bool spraying = false;
		private bool animatingIn = false;
		private bool animatingOut = false;
		private bool waitingForSave = false;
        public float maxWaitTimeForSave = 5;
		private Dictionary<GameObject, Material> materialLookup = new Dictionary<GameObject, Material>();

        private bool Animating { get { return animatingIn || animatingOut; } }

		// Use this for initialization
		void Awake()
		{
			CardboardHead cardboardHead = FindObjectOfType<CardboardHead>();
			if (cardboardHead != null)
				head = cardboardHead.transform;
			
			if (sprayCam == null)
				sprayCam = FindObjectOfType<SprayCam>();

			InstanceMaterials();
			DisableAll();
			if (sprayCam == null || sprayCam.ShouldShowOnBoarding)
				StartOnBoarding();
		}

		void OnEnable()
		{
			if (sprayCam)
			{
				googleAnalytics.LogEvent("UI Interaction", "Onboarding", "Started", 1);

				sprayCam.Spraying += SprayHitCheck;
				sprayCam.WorkInProgressCleared += SprayCam_WorkInProgressCleared;
				sprayCam.SprayCreated += SprayCam_SprayCreated;

			}
		}

		void OnDisable()
		{
			if (sprayCam)
			{
				googleAnalytics.LogEvent("UI Interaction", "Onboarding", "Ended", 1);

				sprayCam.Spraying -= SprayHitCheck;
				sprayCam.WorkInProgressCleared -= SprayCam_WorkInProgressCleared;
				sprayCam.SprayCreated -= SprayCam_SprayCreated;
			}
		}

		private void SprayCam_WorkInProgressCleared()
		{
			Debug.Log("Onboarding disabled due to WIP clear");
			DisableAll();
		}

		private void SprayCam_SprayCreated(ISpray obj)
		{
			waitingForSave = false;
		}

		public void StartOnBoarding()
		{
			StartCoroutine(OnBoardingCoroutine());
		}

		private float SphereCastRadius
		{
			get
			{
				switch (sprayCam.BrushSize)
				{
					case BrushSize.Big: return brushRadii[0];
					case BrushSize.Medium: return brushRadii[1];
					case BrushSize.Small: return brushRadii[2];
				}
				return brushRadii[0];
			}
		}

		public Vector3 SpherePosition
		{
			get { return head.position + head.forward * sphereCastOffset; }
		}

		void SprayHitCheck()
		{
			if (!this.gameObject.activeSelf)
				return;

			if (Physics.CheckSphere(SpherePosition, SphereCastRadius, layersToRaycast.value))
			{
				// only advance the target if we are not animating
				if (!Animating)
					NextTarget();
			}
		}

		public void SprayStart()
		{
			if (!this.gameObject.activeSelf)
				return;

			spraying = true;
			SprayHitCheck();
		}

		public void SprayStop()
		{
			if (!this.gameObject.activeSelf)
				return;
			spraying = false;
		}

		void DisableAll()
		{
			// start by clear all targets
			for (int i = 0; i < sprayTargetGameObjects.Length; i++)
			{
				sprayTargetGameObjects[i].SetActive(false);
			}

			for (int i = 0; i < connectorGameObjects.Length; i++)
			{
				connectorGameObjects[i].SetActive(false);
			}

			rotationNotice.SetActive(false);
			tapNoticeText.gameObject.SetActive(false);
			tapNoticeArrow.gameObject.SetActive(false);
		}

		void NextTarget()
		{
			if (currentSprayTarget != null)
			{
				Debug.Log("Target hit with a spray: " + currentSprayTarget);
				StartCoroutine(AnimateOut(currentConnector, currentSprayTarget, animateOutTime));
				googleAnalytics.LogEvent("UI Interaction", "Onboarding", "Target: " + currentSprayTarget.name + " acheived", 1);
			}

			currentSprayTargetIndex++;
			if (currentSprayTargetIndex > sprayTargetGameObjects.Length - 1)
			{
				currentSprayTargetIndex = -1;
				currentSprayTarget = null;
				currentConnector = null;
				doneWithTargets = true;
				Debug.Log("Spray targets all hit");
			}
			else
			{
				currentSprayTarget = sprayTargetGameObjects[currentSprayTargetIndex];
				currentConnector = connectorGameObjects[currentSprayTargetIndex];
				StartCoroutine(AnimateIn(currentConnector, currentSprayTarget, animateInTime));
			}
		}

		private void InstanceMaterials()
		{
			MeshRenderer mr;
			Material m;
			// this is here to avoid material churn
			for (int i = 0; i < sprayTargetGameObjects.Length; i++)
			{
				mr = sprayTargetGameObjects[i].GetComponentInChildren<MeshRenderer>();
				m = Instantiate(mr.sharedMaterial);
				mr.material = m;
				materialLookup[sprayTargetGameObjects[i]] = m;
			}

			for (int i = 0; i < connectorGameObjects.Length; i++)
			{
				mr = connectorGameObjects[i].GetComponentInChildren<MeshRenderer>();
				m = Instantiate(mr.sharedMaterial);
				mr.material = m;
				materialLookup[connectorGameObjects[i]] = m;
			}

			mr = rotationNotice.GetComponentInChildren<MeshRenderer>();
			m = Instantiate(mr.sharedMaterial); ;
			mr.material = m;
			materialLookup[rotationNotice] = m;
		}

		private Material GetMaterial(GameObject obj)
		{
			// this makes a lot of assumptions so we wrap it up here in case it needs to change
			// this is pretty very specific to the way the on-boarding quads are setup
			return obj.GetComponentInChildren<MeshRenderer>().sharedMaterial;
		}

		IEnumerator AnimateIn(GameObject connector, GameObject sprayTarget, float time)
		{
			animatingIn = true;

			Material cMat = connector != null ? materialLookup[connector] : null;
			Material tMat = sprayTarget != null ? materialLookup[sprayTarget] : null;

			if (connector != null)
				connector.SetActive(true);
			if (sprayTarget != null)
				sprayTarget.SetActive(true);
			float startTime = Time.time;
			while (true)
			{
				float ellapsed = Time.time - startTime;
				float t = Mathf.Clamp01(ellapsed / time);

				// assume the connector has AlphaMultiplySpatial
				// assume the target has AlphaMultiply

				if (cMat != null)
					cMat.SetFloat("_T", inConnectorCurve.Evaluate(t));
				if (tMat != null)
					tMat.SetFloat("_AlphaMult", inSprayAlphaCurve.Evaluate(t));

				if (t == 1.0f)
					break;
				yield return null;
			}

			animatingIn = false;
		}

		IEnumerator AnimateOut(GameObject connector, GameObject sprayTarget, float time)
		{
			animatingOut = true;

			Material cMat = connector != null ? materialLookup[connector] : null;
			Material tMat = sprayTarget != null ? materialLookup[sprayTarget] : null;

			float startTime = Time.time;
			while (true)
			{
				float ellapsed = Time.time - startTime;
				float t = Mathf.Clamp01(ellapsed / time);

				// assume the connector has AlphaMultiplySpatial
				// assume the target has AlphaMultiply
				if (cMat != null)
					cMat.SetFloat("_T", outConnectorCurve.Evaluate(t));
				if (tMat != null)
					tMat.SetFloat("_AlphaMult", outSprayAlphaCurve.Evaluate(t));

				if (t == 1.0f)
					break;
				yield return null;
			}
			if (connector != null)
				connector.SetActive(false);
			if (sprayTarget != null)
				sprayTarget.SetActive(false);
			animatingOut = false;
		}

		// TODO: move to utility library
		public static Quaternion LookRotationUpPriority(Vector3 forward, Vector3 up)
		{
			Vector3 x = Vector3.Cross(up, forward).normalized;
			Vector3 newForward = Vector3.Cross(x, up).normalized;
			return Quaternion.LookRotation(newForward, up);
		}

		IEnumerator OnBoardingCoroutine()
		{
			waitingForSave = true;
			doneWithTargets = false;
			DisableAll();
			yield return new WaitForSeconds(0.5f);
			// align onboard to camera so the onboarding appears in front of the user
			onboardingTransform.rotation = LookRotationUpPriority(head.forward, Vector3.up);

			currentSprayTargetIndex = 0;
			currentSprayTarget = sprayTargetGameObjects[currentSprayTargetIndex];
			currentConnector = connectorGameObjects[currentSprayTargetIndex];
			StartCoroutine(AnimateIn(currentConnector, currentSprayTarget, animateInTime));

			while (!doneWithTargets)
			{
				// wait for all targets to get sprayed...
				yield return null;
			}

			// last connector
			currentConnector = connectorGameObjects[connectorGameObjects.Length - 1];
			StartCoroutine(AnimateIn(currentConnector, rotationNotice, animateInTime));

			// project onto xz plane so we only measure turning around the y-axis
			Vector3 forwardStart = Vector3.ProjectOnPlane(head.forward, Vector3.up);
			// wait for the user to turn X degrees...

			float maxAngle = 0.0f;
			while (true)
			{
				Vector3 forwardNow = Vector3.ProjectOnPlane(head.forward, Vector3.up);
				float angle = Vector3.Angle(forwardStart, forwardNow);
				if (angle > (maxAngle + 1) && Debug.isDebugBuild)
				{
					maxAngle = angle;
					Debug.Log("OnBoarding tracked rotation angle: " + angle);
				}

				if (angle > requiredRotationDegrees)
					break;

				yield return null;
			}


			StartCoroutine(AnimateOut(currentConnector, rotationNotice, animateOutTime));

            float tapNoticeEndTime = Time.time + maxWaitTimeForSave;
			tapNoticeText.gameObject.SetActive(true);
			tapNoticeArrow.gameObject.SetActive(true);

			StartCoroutine(AnimateColorAlpha(tapNoticeText, 0.0f, 1.0f, animateInTime, inSprayAlphaCurve));
			StartCoroutine(AnimateColorAlpha(tapNoticeArrow, 0.0f, 1.0f, animateInTime, inSprayAlphaCurve));

			// wait for a spray creation event...
			while (waitingForSave && Time.time < tapNoticeEndTime)
			{
				yield return null;
			}
			
			// user saved, actually done with onboarding now...
			sprayCam.OnboardingComplete();

			// wait for fade out animation
			StartCoroutine(AnimateColorAlpha(tapNoticeText, 1.0f, 0.0f, animateOutTime, outSprayAlphaCurve));
			yield return StartCoroutine(AnimateColorAlpha(tapNoticeArrow, 1.0f, 0.0f, animateOutTime, outSprayAlphaCurve));
			tapNoticeText.gameObject.SetActive(false);
			tapNoticeArrow.gameObject.SetActive(false);
			Debug.Log("Onboard fade out done, disabling object!");

			currentConnector = null;
			currentSprayTarget = null;
			currentSprayTargetIndex = -1;

			this.gameObject.SetActive(false);
		}

		IEnumerator AnimateColorAlpha(RawImage image, float from, float to, float time, AnimationCurve curve)
		{
			float startTime = Time.time;
			while (true)
			{
				float ellapsed = Time.time - startTime;
				float t = Mathf.Clamp01(ellapsed / time);
				float a = curve.Evaluate(t);

				image.color = new Color(image.color.r, image.color.g, image.color.b, a);
				
				if (t == 1.0f)
					break;
				yield return null;
			}
		}

		public void FadePercent(float p)
		{
			Material m;
			for (int i = 0; i < sprayTargetGameObjects.Length; i++)
			{
				m = materialLookup[sprayTargetGameObjects[i]];
				m.SetFloat("_AlphaMult", p);
			}

			for (int i = 0; i < connectorGameObjects.Length; i++)
			{
				m = materialLookup[connectorGameObjects[i]];
				m.SetFloat("_AlphaMult", p);
			}

			m = materialLookup[rotationNotice];
			m.SetFloat("_AlphaMult", p);

			tapNoticeText.color = new Color(1, 1, 1, p);
		}

#if UNITY_EDITOR
		void OnDrawGizmos()
		{
			if (head != null)
			{
				Gizmos.color = spraying ? Color.red : Color.green;
				Gizmos.DrawWireSphere(SpherePosition, SphereCastRadius);
			}
		}
#endif
	}
}
