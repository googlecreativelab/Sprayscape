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
using System;

public class SimpleTimer
{
	private MonoBehaviour context;
	private float duration = 0.0f;
	private float elapsed = 0.0f;
	private bool running = false;

	public event Action Finished;
	
	public SimpleTimer(MonoBehaviour context, float duration)
	{
		this.context = context;
		this.duration = duration;
	}

	public void Start(bool reset = false)
	{
		if (reset)
		{
			elapsed = 0.0f;
		}
		
		if (!running)
		{
			context.StartCoroutine(Run());
		}
	}
	
	public void Stop()
	{
		running = false;
		elapsed = 0.0f;
	}
	
	public void Pause()
	{
		running = false;
	}
	

	private IEnumerator Run()
	{
		running = true;

		while (running)
		{
			elapsed = elapsed + Time.deltaTime;

			if (elapsed >= duration)
			{
				running = false;
				Finished();
			}
			else
			{
				yield return null;
			}
		}
	}
}
