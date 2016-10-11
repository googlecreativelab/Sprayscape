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

public class LibraryController : MonoBehaviour
{
	#region Inspector

	public SprayCam sprayCam;
	public CameraCapture cameraCapture;
	public ViewManager viewManager;
	public RectTransform contentRoot;
	public ScrollRect scrollRect;
	public RectTransform slideRect;

	public GameObject listElementPrefab;

	#endregion

	#region Private

	private float listTotalHeight;
	private float listVisibleHeight;
	private float listElementHeight;

	// A map from spray IDs to list elements.
	private Dictionary<int, LibraryListElement> elementLookup = new Dictionary<int, LibraryListElement>();

	#endregion

	#region slide animation variables

	private float slideTo = 0.0f;
	private bool sliding = false;
	private float slideSpeed = 0.0f;
	private float slidePosition = 0.0f;

	#endregion

	#region Lifecycle

	void Awake()
	{
		if (sprayCam == null)
		{
			sprayCam = FindObjectOfType<SprayCam>();
		}
	}

	void Start()
	{
		// Update the element size before creating any of the list elements.
		UpdateListElementSize();

		for (int i = 0; i < sprayCam.SavedSprays.Count; i++)
		{
			var spray = sprayCam.SavedSprays[i];
			CreateListElement(spray);
		}

		PositionListElements();
	}

	void OnEnable()
	{
		sprayCam.SprayCreated += SprayCam_SprayCreated;
		sprayCam.SprayDeleted += SprayCam_SprayDeleted;
	}

	void OnDisable()
	{
		sprayCam.SprayCreated -= SprayCam_SprayCreated;
		sprayCam.SprayDeleted -= SprayCam_SprayDeleted;
	}

	#endregion
	
	private void UpdateListElementSize()
	{
		var transform = listElementPrefab.GetComponent<RectTransform>();
		listElementHeight = transform.rect.height;
	}

	private void UpdateListVisibleHeight()
	{
		var transform = scrollRect.GetComponent<RectTransform>();
		listVisibleHeight = transform.rect.height;
	}

	private void UpdateListContentHeight()
	{
		listTotalHeight = sprayCam.SavedSprays.Count * listElementHeight;
		listTotalHeight = Mathf.Max(listTotalHeight, listVisibleHeight);
		contentRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, listTotalHeight);
	}

	private void CreateListElement(ISpray spray)
	{
		spray.OrderChanged += Spray_OrderChanged;

		GameObject go = Instantiate(listElementPrefab);

		go.name = "Photo Id " + spray.Id;
		go.SetActive(false);

		LibraryListElement elem = go.GetComponent<LibraryListElement>();

		elem.spray = spray;
		elem.libraryController = this;

		// use the ScrollRect as the vertical drag handler
		LockedDragHandler lockedDragHandler = elem.GetComponent<LockedDragHandler>();
		lockedDragHandler.verticalEventReceiver = scrollRect.GetComponent<LockedDragForwarder>();

		int x = PhotoIO.GetThumbnailX(spray.Id);
		int y = PhotoIO.GetThumbnailY(spray.Id);

		// we need to deal with the half texel offset, but this creates a 1 pixel discontinuity, since the atan2 edge should overlap
		Vector4 texParams = new Vector4(x * PhotoIO.ThumbnailSizeX + PhotoIO.HalfPixel,
										y * PhotoIO.ThumbnailSizeY + PhotoIO.HalfPixel,
										PhotoIO.PanoScalingX * PhotoIO.ThumbnailSizeX,
										PhotoIO.PanoScalingY * PhotoIO.ThumbnailSizeY);

		// set the spray texture
		elem.sprayCanvas.material = Instantiate(elem.sprayCanvas.material); // clone material for unique _TexParams
		elem.sprayCanvas.material.SetVector("_TexParams", texParams);
		elem.sprayCanvas.texture = PhotoIO.thumbnails;

		// set the label text
		elem.labelText.text = spray.Label;

		// anchor the list element to the content rect
		elem.scrollTransform.SetParent(contentRoot);
		elem.scrollTransform.offsetMin = new Vector2(0, elem.scrollTransform.offsetMin.y);
		elem.scrollTransform.offsetMax = new Vector2(0, elem.scrollTransform.offsetMax.y);
		elem.scrollTransform.anchoredPosition = new Vector2(0, 0);
		elem.scrollTransform.localScale = Vector3.one;

		elem.DrawerPeeked += LibraryListElement_DrawerPeeked;
		elem.DrawerOpened += LibraryListElement_DrawerOpened;
		
		elementLookup[spray.Id] = elem;
	}

	private void PositionListElements()
	{
		for (int i = 0, n = sprayCam.SavedSprays.Count; i < n; i++)
		{
			var spray = sprayCam.SavedSprays[i];
			var elem  = elementLookup[spray.Id];

			var scrollTransform = elem.scrollTransform;
			var scrollOffset = (n - spray.Order - 1) * listElementHeight;

			scrollTransform.SetAnchoredVerticalPosition(-scrollOffset);
		}
	}

	private void DestroyListElement(ISpray spray)
	{
		spray.OrderChanged -= Spray_OrderChanged;

		var elem = elementLookup[spray.Id];

		if (elem)
		{
			elem.DrawerPeeked -= LibraryListElement_DrawerPeeked;
			elem.DrawerOpened -= LibraryListElement_DrawerOpened;
		}

		if (elem && elem.gameObject)
		{
			Destroy(elem.gameObject);
		}

		elementLookup.Remove(spray.Id);
	}

	#region SprayCam Event Handlers

	private void SprayCam_SprayCreated(ISpray spray)
	{
		CreateListElement(spray);
		PositionListElements();
		UpdateListContentHeight();
		DeactivateHiddenListElements();
	}

	private void SprayCam_SprayDeleted(ISpray spray)
	{
		DestroyListElement(spray);
		PositionListElements();
		UpdateListContentHeight();
		DeactivateHiddenListElements();
	}

	private void Spray_OrderChanged(ISpray spray, int order)
	{
		PositionListElements();
	}

	#endregion

	#region LibraryListElement Event Handlers

	private void LibraryListElement_DrawerPeeked(LibraryListElement elem)
	{
		scrollRect.StopMovement();
		
		for (int i = 0; i < contentRoot.childCount; i++)
		{
			var childTransform = contentRoot.GetChild(i);

			if (childTransform == elem.transform)
			{
				continue;
			}
			else if (childTransform.gameObject.activeSelf)
			{
				// TODO looped component access could be sped up by keeping list elements in a List object
				var childListElement = childTransform.GetComponent<LibraryListElement>();
				childListElement.CloseDrawers();
			}
		}
	}

	private void LibraryListElement_DrawerOpened(LibraryListElement elem)
	{

	}

	#endregion

	public void DeactivateHiddenListElements()
	{
		float listScrollPosition = contentRoot.anchoredPosition.y + listTotalHeight * 0.5f;

		int min = Mathf.FloorToInt(listScrollPosition / listElementHeight);
		int max = min + Mathf.FloorToInt(listVisibleHeight / listElementHeight) + 1;

		for (int i = 0; i < contentRoot.childCount; i++)
		{
			var child = contentRoot.GetChild(i);
			var order = contentRoot.childCount - i - 1;

			if (order >= min && order <= max)
			{
				child.gameObject.SetActive(true);
			}
			else
			{
				child.gameObject.SetActive(false);
			}
		}
	}
	
	public void OnScroll()
	{
		DeactivateHiddenListElements();
	}
	
	public void ClickElement(LibraryListElement element)
	{
		// TODO: make sure wip is saved before loading the new image...
		sprayCam.PreviewSpray(element.spray);
		viewManager.ShowPreview();
	}

	public void DeleteElement(LibraryListElement element)
	{
		sprayCam.DeleteSpray(element.spray);
	}

	public void ShareElement(LibraryListElement element)
	{
		viewManager.ShowShareChoice(element.spray);
	}

	public void ShowMenu()
	{
		// These don't get valid results until the related rect transforms are
		// enabled, so this is a safe place to update them.
		UpdateListVisibleHeight();
		UpdateListContentHeight();

		contentRoot.SetAnchoredVerticalPosition(listTotalHeight * -0.5f);

		// Activate/deactivate by visiblity with the new scroll position.
		DeactivateHiddenListElements();

		slideTo = 0.0f;
		slideSpeed = 0.0f;
		slidePosition = 1080.0f;
		
		for (int i = 0, n = contentRoot.childCount; i < n; i++)
		{
			var childTransform = contentRoot.GetChild(i);
			var childListElement = childTransform.GetComponent<LibraryListElement>();

			childListElement.CloseDrawers(false);
		}

		if (!sliding)
		{
			StartCoroutine(SlideMenu());
		}
	}

	public void HideMenu()
	{
		slideTo = 1080.0f;
		slideSpeed = 0.0f;
		slidePosition = 0.0f;

		if (!sliding)
		{
			StartCoroutine(SlideMenu());
		}
	}
	
	IEnumerator SlideMenu()
	{
		sliding = true;

		while (true)
		{
			slidePosition = Smoothing.SpringSmooth(slidePosition, slideTo, ref slideSpeed, 0.16f, Time.deltaTime);
			slideRect.anchoredPosition = new Vector2(slidePosition, slideRect.anchoredPosition.y);

			if (Mathf.Abs(slidePosition - slideTo) < 0.01f)
				break;

			yield return null;
		}

		// snap to final position
		slideRect.SetAnchoredHorizontalPosition(slideTo);
		sliding = false;
	}

	void Update()
	{
		
	}
}
