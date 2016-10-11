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

public class FPSCounter : MonoBehaviour {

	public Material mat;
	const int FRAME_COUNT = 60;
	float[] frameTimes= new float[FRAME_COUNT];
	int idx;
	float averageTimePerFrame = 0;
	float max;


	void LateUpdate(){
		
		frameTimes[idx] = Time.realtimeSinceStartup;
		
		max = 0;
		averageTimePerFrame = 0;
		for(int i=0; i<FRAME_COUNT-1; i++){
			int frame = (idx + i +1)%FRAME_COUNT;
			float dt = frameTimes[ (frame+1)%FRAME_COUNT] - frameTimes[frame];
			
			if(dt > max){
				max = dt;
			}
			averageTimePerFrame += dt;
		}
		averageTimePerFrame /= (float)FRAME_COUNT;

		idx = (idx+1)%FRAME_COUNT;
		
	}


	void OnPostRender(){
		DrawProfiler();
	}
	/*void OnGUI() {
		if(averageTimePerFrame > 0){
			int framerate = (int)(1.0f / averageTimePerFrame) ;
			int slowest = (int)(max * 1000) ;
			string fps = framerate + " fps, max = " + slowest+ "ms";
			GUILayout.Label(fps,"box");
			
		}
	}*/


	void DrawProfiler(){
		if(mat != null){


			GL.PushMatrix();
	        mat.SetPass(0);
	        GL.LoadOrtho();
	        float height = 60f / Screen.height;
	        float padding = 2f / Screen.height;


	        float width = 5f / Screen.width;
	        float wpad = 1.5f / Screen.width;
	        float x = 2*(width + wpad);
	        GL.Begin(GL.QUADS);
	        for(int i=0; i<FRAME_COUNT; i++){
				int frame = (idx + i )%FRAME_COUNT;
				float dt = frameTimes[ (frame+1)%FRAME_COUNT] - frameTimes[frame];
				float val = dt / (1/60f);

				if(val > 0){
					
					if(val <=1.01f){
						GL.Color(new Color(0, 0.75f, 1, 1));
					}
					else if(val <=2){
						Color col = (2-val)*new Color(0.5F, 1, 0.5F, 1) + (val-1)*new Color(1, 1, 0.5F, 1);
						GL.Color(col);
					}
					else{
						
						GL.Color(new Color(1, 0, 0, 1));
					
					}
					float hVal = 1-Mathf.Clamp01(val*0.25f);
			        
			        GL.Vertex3(x-width, 1-padding-height, 0);
			        GL.Vertex3(x-width, 1-padding-hVal*height, 0);
			        GL.Vertex3(x, 1-padding-hVal*height, 0);
			        GL.Vertex3(x, 1-padding-height, 0);
			        x = x+width+wpad;
			    }
		    }
		    float xStart = width + wpad;
		    float xEnd = x-width-wpad;
		    float line = 2f / Screen.height;
		    float sixty = 0.75f*height;
		    GL.Color(new Color(0.95F, 0.95f, 0.95F, 1));
		    GL.Vertex3(xStart, 1-padding-sixty-line, 0);
		    GL.Vertex3(xStart, 1-padding-sixty, 0);
		    GL.Vertex3(xEnd, 1-padding-sixty, 0);
		    GL.Vertex3(xEnd, 1-padding-sixty-line, 0);


	        GL.End();
	        

	        GL.PopMatrix();
	    }
	}
	/*void ApplyToText(){
		if(averageTimePerFrame > 0){
			int framerate = (int)(1.0f / averageTimePerFrame) ;
			string fps = framerate + " fps, max = " + max;
			txt.text = fps;
		}
		
	}*/

}
