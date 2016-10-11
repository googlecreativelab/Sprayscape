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
using System;

public enum DragLockDirection
{
	None,
	Vertical,
	Horizontal,
}

public interface ILockedDragReceiver
{
	void OnDrag(PointerEventData eventData);
	void OnEndDrag(PointerEventData eventData);
	void OnBeginDrag(PointerEventData eventData);
	void OnPointerClick(PointerEventData eventData);
}

public class LockedDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
	#region Inspector
	
	// TODO the deadzone could be a Vector2 with a little more math
	[Tooltip("The size of the deadzone where drag events won't be forwarded, and the orientation is not locked.")]
	public float deadzone = 5f;

	public MonoBehaviour clickEventReceiver;
	public MonoBehaviour verticalEventReceiver;
	public MonoBehaviour horizontalEventReceiver;

	#endregion

	#region Members
	
	private int dragID;
	private Vector2 dragStart;
	private Vector2 dragPrevious;
	private DragLockDirection dragLock;
	private ILockedDragReceiver dragEventReceiver = null;

	#endregion
	
	#region Drag Handlers

	public void OnBeginDrag(PointerEventData data)
	{
		dragID = data.pointerId;
		dragLock = DragLockDirection.None;
		dragStart = data.position;
		dragPrevious = data.position;
		
		// don't forward the BeginDrag event until the user leaves the deadzone
	}

	public void OnDrag(PointerEventData data)
	{
		// Forwarding the drag events mutates the event data position, but we
		// need the original value to assign it to the dragPrevious position
		// after evaluating this drag event.
		Vector2 originalPosition = data.position;

		if (dragID != data.pointerId)
		{
			return;
		}
		else if (dragLock == DragLockDirection.None)
		{
			Vector2 delta = data.position - dragStart;
			
			// start locking the drag if the user has dragged further than the deadzone radius
			if ((deadzone * deadzone) < delta.sqrMagnitude)
			{
				float absx = Mathf.Abs(delta.x);
				float absy = Mathf.Abs(delta.y);

				dragLock = (absx < absy) ? DragLockDirection.Vertical : DragLockDirection.Horizontal;

				if (Debug.isDebugBuild)
				{
					string direction = (dragLock == DragLockDirection.Vertical) ? "vertical" : "horizontal";
					Debug.Log("Locking drag to " + direction);
				}

				// keep a cast reference to the event handler for this direction
				dragEventReceiver = (dragLock == DragLockDirection.Horizontal ? horizontalEventReceiver : verticalEventReceiver) as ILockedDragReceiver;

				// forward the BeginDrag event now that we're locked to one direction
				ForwardBeginDrag(data);
			}
		}
		else
		{
			ForwardDragEvent(data);
		}

		dragPrevious = originalPosition;
	}

	public void OnEndDrag(PointerEventData data)
	{
		if (dragID != data.pointerId)
		{
			return;
		}
		else if (dragLock != DragLockDirection.None)
		{
			ForwardEndDrag(data);
		}
		
		dragLock = DragLockDirection.None;
		dragStart = Vector2.zero;
		dragPrevious = Vector2.zero;
	}

	public void OnPointerClick(PointerEventData data)
	{
		// If the lock direction is still none, we never left the deadzone,
		// and never started a drag, so lets count it as a click.
		
		if (dragLock == DragLockDirection.None)
		{
			ForwardPointerClick(data);
		}
	}
	
	#endregion
	
	#region Event Forwarding

	private void ForwardBeginDrag(PointerEventData data)
	{
		data.position = CorrectedPosition(data.position);
		
		if (dragEventReceiver != null)
		{
			dragEventReceiver.OnBeginDrag(data);
		}
	}

	private void ForwardDragEvent(PointerEventData data)
	{
		data.position = CorrectedPosition(data.position);

		if (dragEventReceiver != null)
		{
			dragEventReceiver.OnDrag(data);
		}
	}

	private void ForwardEndDrag(PointerEventData data)
	{
		data.position = CorrectedPosition(data.position);

		if (dragEventReceiver != null)
		{
			dragEventReceiver.OnEndDrag(data);
		}
	}

	private void ForwardPointerClick(PointerEventData data)
	{
		var receiver = clickEventReceiver as ILockedDragReceiver;

		if (receiver != null)
		{
			receiver.OnPointerClick(data);
		}
	}

	#endregion
	
	private Vector2 CorrectedPosition(Vector2 position)
	{
		// use the position where the drag started to lock the drag to a single axis
		float x = (dragLock == DragLockDirection.Horizontal) ? position.x : dragStart.x;
		float y = (dragLock == DragLockDirection.Vertical)   ? position.y : dragStart.y;

		return new Vector2(x, y);
	}
}
