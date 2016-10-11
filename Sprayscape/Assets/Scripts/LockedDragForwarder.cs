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
using UnityEngine.EventSystems;

// This connects the ILockedDragReceiver interface to all four of the
// related UnityEngine.EventSystems interfaces. We need to use our own
// event interface to avoid duplicate drag events in the case when the
// LockedDragHandler game object is also one of its receivers. However,
// we still want to let the LockedDragHandler interface with native
// Unity UI objects like ScrollRect. This behaviour fills the gap.

public class LockedDragForwarder : MonoBehaviour, ILockedDragReceiver
{
	public MonoBehaviour target;

	private IDragHandler dragHandler;
	private IEndDragHandler endDragHandler;
	private IBeginDragHandler beginDragHandler;
	private IPointerClickHandler pointerClickHandler;

	void Awake()
	{
		dragHandler         = target as IDragHandler;
		endDragHandler      = target as IEndDragHandler;
		beginDragHandler    = target as IBeginDragHandler;
		pointerClickHandler = target as IPointerClickHandler;
	}

	public void OnBeginDrag(PointerEventData data)
	{
		if (beginDragHandler != null)
		{
			beginDragHandler.OnBeginDrag(data);
		}
	}

	public void OnDrag(PointerEventData data)
	{
		if (dragHandler != null)
		{
			dragHandler.OnDrag(data);
		}
	}

	public void OnEndDrag(PointerEventData data)
	{
		if (endDragHandler != null)
		{
			endDragHandler.OnEndDrag(data);
		}
	}

	public void OnPointerClick(PointerEventData data)
	{
		if (pointerClickHandler != null)
		{
			pointerClickHandler.OnPointerClick(data);
		}
	}
}
