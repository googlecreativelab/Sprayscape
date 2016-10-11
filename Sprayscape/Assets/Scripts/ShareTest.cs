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
using System.IO;

public class ShareTest : MonoBehaviour {

	public TextMesh textMesh;
	public Renderer ren;
	public string dialogText = "SHARE ME";
	public string filePath = "Pictures";
	public string fileName = "equirectangular.jpg";
	public bool runOnStart = false;

	private string texturePath = "file:///sdcard/";
	private string sharePath = "/storage/emulated/0/";
	private string textureUrl = "";
	private string shareUrl = "";

	void Start() {
		// "jar:file://" + Application.dataPath + "!/assets/";
		// "/storage/emulated/0/Android/data/com.creativelab.fishbowl/files/"
		textureUrl = Path.Combine(texturePath, Path.Combine(filePath, fileName));
		shareUrl = Path.Combine(sharePath, Path.Combine(filePath, fileName));

		if (runOnStart) {
			doShare();
		}
	}
	
	void Update() {
		if (Cardboard.SDK.Triggered) {
			doShare();
		}
	}

	public void doShare() {
		#if UNITY_ANDROID
		StartCoroutine(loadTex());
		//Prime31.EtceteraAndroid.shareImageWithNativeShareIntent(shareUrl, dialogText);
		textMesh.text = shareUrl + "\n" + textureUrl;
		#endif
	}

	IEnumerator loadTex() {
		WWW www = new WWW(textureUrl); 
		yield return www;

		ren.material.mainTexture = www.texture;;
	}

}
