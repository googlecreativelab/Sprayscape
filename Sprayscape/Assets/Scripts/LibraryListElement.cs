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
using UnityEngine.EventSystems;
using System;
using System.Collections;

public enum DrawerState
{
	RightDrawerOut,
	LeftDrawerOut,
	None
}

public class LibraryListElement : MonoBehaviour, ILockedDragReceiver
{
	#region Inspector

	public LibraryController libraryController;

	[Tooltip("The transform used to offset the list element when scrolling the list.")]
	public RectTransform scrollTransform;

	[Tooltip("The transform used to offset the list element when exposing the drawers.")]
	public RectTransform swipeTransform;
	
	public RectTransform rightDrawer;
	public RectTransform leftDrawer;
	public RawImage sprayCanvas;
	public Text labelText;

	public ScrollRect scrollRect;
	
	public float maxSwipe = 200.0f;
	public float swipeTippingPoint = 100.0f;
	public AnimationCurve inCurve;
	public AnimationCurve outCurve;
	public float inTime = 0.3f;
	public float outTime = 0.3f;

	public ISpray spray;

	#endregion

	#region Properties

	public DrawerState DrawerState { get; private set; }

	#endregion

	#region Events

	public event Action<LibraryListElement> DrawerPeeked;
	public event Action<LibraryListElement> DrawerOpened;

	#endregion

	private float dragMin;
	private float dragMax;
	private float dragOffset;
	private Vector2 dragStart;
	private DrawerState dragStartState = DrawerState.None;

	// a bunch of state to allow the animating co-routine to be reset and keep running with starting a new co-routine (ugh)
	private bool animating = false;
	private float startX = 0.0f;
	private float targetX = 0.0f;
	private float animationStartTime = 0.3f;
	private float animationTime = 0.3f;
	private AnimationCurve currentCurve;
	
	void Awake()
	{
		if (scrollTransform == null)
		{
			scrollTransform = GetComponent<RectTransform>();
		}
	}
	
	private void SetAnimationTarget(float targetX, AnimationCurve curve, float time)
	{
		this.animationStartTime = Time.time;
		this.animationTime = time;
		this.startX = swipeTransform.anchoredPosition.x;
		this.targetX = targetX;
		this.currentCurve = curve;

		if (!animating)
		{
			StartCoroutine(AnimateTransform());
		}
		// else the co-routine is already running
	}

	private IEnumerator AnimateTransform()
	{
		animating = true;

		while (animating)
		{
			float ellapsed = Time.time - animationStartTime;
			float t = Mathf.Clamp01(ellapsed / animationTime);
			float p = currentCurve.Evaluate(t);
			float x = Mathf.LerpUnclamped(startX, targetX, p);

			swipeTransform.SetAnchoredHorizontalPosition(x);

			if (t == 1.0f) break;
			
			yield return null;
		}

		leftDrawer.gameObject.SetActive(DrawerState == DrawerState.LeftDrawerOut);
		rightDrawer.gameObject.SetActive(DrawerState == DrawerState.RightDrawerOut);
		
		animating = false;
	}

	#region Drawer Control

	public void OpenLeftDrawer(bool animate = true)
	{
		if (animate)
		{
			SetAnimationTarget(maxSwipe, inCurve, inTime);
		}
		else
		{
			swipeTransform.SetAnchoredHorizontalPosition(-maxSwipe);

			leftDrawer.gameObject.SetActive(true);
			rightDrawer.gameObject.SetActive(false);
		}

		DrawerState = DrawerState.LeftDrawerOut;

		if (DrawerOpened != null)
		{
			DrawerOpened(this);
		}
	}

	public void OpenRightDrawer(bool animate = true)
	{
		if (animate)
		{
			SetAnimationTarget(-maxSwipe, inCurve, inTime);
		}
		else
		{
			swipeTransform.SetAnchoredHorizontalPosition(-maxSwipe);

			leftDrawer.gameObject.SetActive(false);
			rightDrawer.gameObject.SetActive(true);
		}

		DrawerState = DrawerState.RightDrawerOut;

		if (DrawerOpened != null)
		{
			DrawerOpened(this);
		}
	}

	public void CloseDrawers(bool animate = true)
	{
		if (animate)
		{
			SetAnimationTarget(0, outCurve, outTime);
		}
		else
		{
			swipeTransform.SetAnchoredHorizontalPosition(0);

			leftDrawer.gameObject.SetActive(false);
			rightDrawer.gameObject.SetActive(false);
		}

		DrawerState = DrawerState.None;
	}

	#endregion

	#region Drag Handlers

	public void OnBeginDrag(PointerEventData eventData)
	{
		dragStart = eventData.position;

		// prevent a single draw from jumping more than 1 state
		switch (DrawerState)
		{
			case DrawerState.RightDrawerOut:
				dragOffset = -maxSwipe;
				dragMin = -maxSwipe;
				dragMax = 0;
				break;
			case DrawerState.LeftDrawerOut:
				dragOffset = maxSwipe;
				dragMin = 0;
				dragMax = maxSwipe;
				break;
			default:
				dragOffset = 0;
				dragMin = -maxSwipe;
				dragMax = maxSwipe;
				break;
		}

		dragStartState = DrawerState;

		// activate both drawers for peeking
		leftDrawer.gameObject.SetActive(true);
		rightDrawer.gameObject.SetActive(true);

		if (DrawerPeeked != null)
		{
			DrawerPeeked(this);
		}
	}

	public void OnDrag(PointerEventData data)
	{
		Vector2 delta = data.position - dragStart;
		float x = Mathf.Clamp(delta.x + dragOffset, dragMin, dragMax);

		swipeTransform.SetAnchoredHorizontalPosition(x);

		// stop the co-routine if its running
		animating = false;
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		Vector2 diff = eventData.position - dragStart + new Vector2(dragOffset, 0);

		if (diff.x > swipeTippingPoint && dragStartState != DrawerState.RightDrawerOut)
		{
			OpenLeftDrawer();
		}
		else if (diff.x < -swipeTippingPoint && dragStartState != DrawerState.LeftDrawerOut)
		{
			OpenRightDrawer();
		}
		else
		{
			CloseDrawers();
		}
	}

	public void OnPointerClick(PointerEventData data)
	{
		var target = data.pointerPressRaycast.gameObject.transform;
		
		if ((target != leftDrawer) && (target != rightDrawer))
		{
			Preview();
		}
	}

	#endregion

	public void Preview()
	{
		if (libraryController != null)
		{
			libraryController.ClickElement(this);
		}
	}

	public void Delete()
	{
		if (libraryController != null)
		{
			libraryController.DeleteElement(this);
		}
	}

	public void Share()
	{
		if (libraryController != null)
		{
			libraryController.ShareElement(this);
		}
	}
}
