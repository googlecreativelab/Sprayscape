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

public class CameraShaderUpdater : MonoBehaviour
{
	public Transform head;

	private Quaternion offset = Quaternion.Euler(0, -90, 0);

	void Awake()
	{
		if (head == null)
		{
			head = this.transform;
		}
	}

	void LateUpdate()
	{
		Shader.SetGlobalVector("_CamUp", offset * head.up);
		Shader.SetGlobalVector("_CamRight", offset * head.right);
		Shader.SetGlobalVector("_CamForward", offset * head.forward);
	}
}
