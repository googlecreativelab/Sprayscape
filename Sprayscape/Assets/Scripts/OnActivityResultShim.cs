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

public class OnActivityResultShim : MonoBehaviour {

	public ViewManager viewManager;
	public SprayCam sprayCam;

	// Use this for initialization
	void Awake () {
		if (viewManager == null)
			viewManager = FindObjectOfType<ViewManager>();

		if (sprayCam == null)
			sprayCam = FindObjectOfType<SprayCam>();
	}

	void OnActivityResult(string argString)
	{
		string[] res = argString.Split(',');
		int requestCode = -1;
		int resultCode = -1;
		int.TryParse(res[0], out requestCode);
		int.TryParse(res[1], out resultCode);
		string intentAsString = res[2]; // this is probably useless...

		sprayCam.OnActivityResult(requestCode, resultCode, intentAsString);
		viewManager.OnActivityResult(requestCode, resultCode, intentAsString);
	}
}
