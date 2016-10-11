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

using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class GoogleDrivePlugin : MonoBehaviour {

	public void Start() {

	}

	public void Upload(string ImagePath, string FileName) {
		#if UNITY_IOS
		_GoogleDrivePlugin_UploadFile(ImagePath, FileName);
		#endif
	}

	#if UNITY_IOS
	[DllImport("__Internal")]
	private static extern void _GoogleDrivePlugin_UploadFile (string iosPath, string fileName);
	#endif
}
