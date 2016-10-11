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

public static class RectTransformExtensions
{
	public static void SetAnchoredHorizontalPosition(this RectTransform rect, float x)
	{
		rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
	}

	public static void SetAnchoredVerticalPosition(this RectTransform rect, float y)
	{
		rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
	}
}
