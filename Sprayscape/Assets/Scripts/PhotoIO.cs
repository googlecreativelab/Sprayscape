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
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine.UI;

public class PhotoIO {


	///presently a max of 128 save slots, because of thumbnail caching for performance
	public const int MAX_FILES = 128;

	public static Texture2D thumbnails;

	static public bool[] saveSlots = new bool[MAX_FILES];
	static public List<int> saveMap = new List<int>(128);

	// A buffer to copy the sprayComposite into so it can be saved to disk.
	// RenderTexture objects can't be used with PhotoIO.SaveWhatever.
	static public Texture2D saveBuffer;

	public static int FileCount{
		get{
			if(!prefsLoaded){
				LoadFilePaths();
			}
			return saveMap.Count;
		}
	}
	public static int GetFileID(int idx){
		if(!prefsLoaded){
			LoadFilePaths();
		}
		if(idx >-1 && idx < saveMap.Count){
			return saveMap[idx];
		}
		else{
			return -1;
		}
	
	}


	static byte[] EncodeToJPG(Texture2D tex){
		if(tex != null){
			byte[] bytes = tex.EncodeToJPG(80);
			return bytes;
		}
		return null;
	}

	public static int GetThumbnailX(int fileIndex){
		return fileIndex%8;
	}
	public static int GetThumbnailY(int fileIndex){
		return fileIndex/8;	
	}

	public const int ThumbWidth = 512;
	public const int ThumbHeight = 256;

	public const int ThumbnailCountX = 8;
	public const int ThumbnailCountY = 16;
	public const float ThumbnailSizeX = 1.0f/ThumbnailCountX;
	public const float ThumbnailSizeY = 1.0f/ThumbnailCountY;

	///we need to deal with the half texel offset, but this creates a 1 pixel discontinutity, since the atan2 edge should overlap
	public const float HalfPixel = (0.5f/4096.0f);
	public const float PanoScalingX = (511.0f/512.0f);
	public const float PanoScalingY = (255.0f/256.0f);
    

    ///binary header
/// see http://www.fileformat.info/format/jpeg/egff.htm for more info on what this means
/*
ffd8 ffe0 0010 4a46 4946 0001 0100 0001
0001 0000 
*/

//// Binary exif data to add 'make:Ricoh, model: ricoh theta s' - inserted after jpg header
/*
	ffe1 005a 4578 6966 0000 4d4d
002a 0000 0008 0004 010f 0002 0000 0006
0000 003e 0110 0002 0000 000e 0000 0044
0128 0003 0000 0001 0001 0000 0213 0003
0000 0001 0001 0000 0000 0000 5269 636f
6800 5249 434f 4820 5448 4554 4120 5300

*/

	static readonly byte[] RicohEXIF = new byte[]{
		0xff,
		0xe1,
		0x00,
		0x5a,
		0x45,
		0x78,
		0x69,
		0x66,
		0x00,
		0x00,
		0x4d,
		0x4d,
		0x00,
		0x2a,
		0x00,
		0x00,
		0x00,
		0x08,
		0x00,
		0x04,
		0x01,
		0x0f,
		0x00,
		0x02,
		0x00,
		0x00,
		0x00,
		0x06,
		0x00,
		0x00,
		0x00,
		0x3e,
		0x01,
		0x10,
		0x00,
		0x02,
		0x00,
		0x00,
		0x00,
		0x0e,
		0x00,
		0x00,
		0x00,
		0x44,
		0x01,
		0x28,
		0x00,
		0x03,
		0x00,
		0x00,
		0x00,
		0x01,
		0x00,
		0x01,
		0x00,
		0x00,
		0x02,
		0x13,
		0x00,
		0x03,
		0x00,
		0x00,
		0x00,
		0x01,
		0x00,
		0x01,
		0x00,
		0x00,
		0x00,
		0x00,
		0x00,
		0x00,
		0x52,
		0x49,
		0x43,
		0x4f,
		0x48,
		0x00,
		0x52,
		0x49,
		0x43,
		0x4f,
		0x48,
		0x20,
		0x54,
		0x48,
		0x45,
		0x54,
		0x41,
		0x20,
		0x53,
		0x00
	};

	public static byte[] Append360Exif(byte[] bytes){
		byte[] newBytes = new byte[bytes.Length + RicohEXIF.Length];

		int index = 0;
		for(int i=0; i<20; i++){
			newBytes[index] = bytes[i];
			index +=1;
		}

		for(int i=0; i<RicohEXIF.Length; i++){
			newBytes[index] = RicohEXIF[i];
			index +=1;
		}

		for(int i=20; i<bytes.Length; i++){
			newBytes[index] = bytes[i];
			index +=1;
		}
		return newBytes;
	}

    
	public static int SaveToLocalStore(RenderTexture tex){
		if(tex != null){
			RenderTexture previous = RenderTexture.active;
			RenderTexture.active = tex;

			if (PhotoIO.saveBuffer == null) {
				PhotoIO.saveBuffer = new Texture2D(2048, 1024);
			}
			// Copy the current spray into the texture buffer and thumbnail.
			PhotoIO.saveBuffer.ReadPixels(new Rect(0, 0, 2048, 1024), 0, 0);

			RenderTexture.active = previous;

			byte[] bytes = PhotoIO.saveBuffer.EncodeToJPG(100);
			bytes = PhotoIO.Append360Exif(bytes);
		
			if(bytes != null){

				Debug.Log(Application.persistentDataPath);
				//------------------------------------------------
				int nextID = GetNextID();
				string filePath = Path(nextID);
				//------------------------------------------------
				InsertThumbnail(tex, nextID);
				SaveThumbnailImage();
				//------------------------------------------------

				//SaveFilePaths();

				try{
					File.WriteAllBytes(filePath, bytes);
				}
				catch(System.ArgumentException){
					return -1;
				}
				catch(System.IO.IOException){
					return  -1;
				}
				catch(System.UnauthorizedAccessException){
					return  -1;
				}
				catch(System.NotSupportedException){
					return -1;
				}
				catch(System.Security.SecurityException){
					return  -1;
				}

				return nextID;
			}
		}

		return -1;
	}


	//static List<int> fileIDs = new List<int>();
	const string FILE_COUNT = "FileCount";
	const string FILE_NAME = "Sprayscape_";
	const string THUMBNAILS = "sprayscape_thumbnails";

	static bool prefsLoaded = false;

	public static void LoadFilePaths(){
		Debug.Log("load prefs");
		
		string path;

		for(int i=0; i<MAX_FILES; i++){
			path = Path(i);
			if(File.Exists(path)){
				saveSlots[i]= true;
				saveMap.Add(i);
			}
			else{
				saveSlots[i]= false;
			}
		}

		prefsLoaded = true;
	}

	/// fixed size array for files (128)
	/// at each point, a file exists or not
	/// load up a list with pointers into that array

	public static int GetNextID(){
		if(!prefsLoaded){
			LoadFilePaths();
		}

		
		for(int i=0; i < saveSlots.Length; i++){
			if(saveSlots[i] == false){
				saveSlots[i] = true;
				if(!saveMap.Contains(i)){
					saveMap.Add(i);
				}
				return i;
			}
		}
		return -1;
	}

	internal static string PhotoLabel(int idx)
	{
		string path = Path(idx);
		DateTime d = File.GetLastWriteTime(path);
		return string.Format("{0:D2}/{1:D2}/{2:D4}\n{3}", d.Month, d.Day, d.Year, d.ToString("hh:mm tt"));
	}

	public static string ThumbnailPath(){
		return Application.temporaryCachePath + "/" + THUMBNAILS+".jpg";
	}

	public static string Path(int idx){
		return Application.persistentDataPath + "/" + FILE_NAME+idx+".jpg";
	}

	public static void DeleteImage(int idx){
		if(!prefsLoaded){
			LoadFilePaths();
		}
			
		string path = Path(idx);
		if(File.Exists(path)){
			File.Delete(path);
		}

		if(idx >-1 && idx < saveSlots.Length){
			saveSlots[idx] = false;

			for(int i=saveMap.Count-1; i>-1; i--){
				if(saveMap[i] ==idx){
					saveMap.RemoveAt(i);
				}
			}
		}
	}

	public static bool LoadImage(ref Texture2D t, int idx){

		if(!prefsLoaded){
			LoadFilePaths();
		}
		
		string path = Path(idx);
		if(File.Exists(path)){
			byte[] bytes = File.ReadAllBytes(path);
			if(bytes != null){
	        	bool success = t.LoadImage(bytes);
	        	if(success){
	        		return true;
	        	}
        	}
        	return false;
		}

		return false;
	}

	public static byte[] EncodeThumbnailToPNG(){
		return thumbnails.EncodeToPNG();
	}

	public static byte[] EncodeThumbnailToJPG(){
		return thumbnails.EncodeToJPG();
	}

	public static byte[] EncodeTextureRaw(){
		return thumbnails.GetRawTextureData();
	}


	static void InsertThumbnail(RenderTexture texture, int idx){
		RenderTexture rt = RenderTexture.GetTemporary(ThumbWidth, ThumbHeight);

		RenderTexture previous = RenderTexture.active;
		RenderTexture.active = rt;

		// Copy the spray into the thumbnail texture buffer and thumbnail.
		Graphics.Blit(texture, rt);

		int startX = ThumbWidth * (idx % 8);
		int startY = ThumbHeight * (idx / 8);

		// TODO why was this commented out?
		//Graphics.CopyTexture(rt, 0,0,0,0,rt.width, rt.height, thumbnails, 0,0,startX, startY);

		thumbnails.ReadPixels(new Rect(0, 0, ThumbWidth, ThumbHeight), startX, startY);
		thumbnails.Apply();
	
		RenderTexture.active = previous;

		RenderTexture.ReleaseTemporary (rt);
	}

	static void SaveThumbnailImage(){
//		byte[] bytes = EncodeThumbnailToPNG();
//		byte[] bytes = EncodeTextureRaw();
		byte[] bytes = EncodeThumbnailToJPG();
		try{
			Debug.Log("Write thumbnails to " + ThumbnailPath());
			File.WriteAllBytes(ThumbnailPath(), bytes);
		}
		catch(System.ArgumentException){
			return ;
		}
		catch(System.IO.IOException){
			return  ;
		}
		catch(System.UnauthorizedAccessException){
			return  ;
		}
		catch(System.NotSupportedException){
			return ;
		}
		catch(System.Security.SecurityException){
			return  ;
		}
	}

	public static void LoadThumbnailImage(){
		if (thumbnails == null || thumbnails.width != 4096) {
			AllocateThumbnails ();
		}
		
		string path = ThumbnailPath();
		if (File.Exists (path)) {
			byte[] bytes = File.ReadAllBytes (path);
			try {
				thumbnails.LoadImage (bytes);
				thumbnails.Apply ();
				Debug.Log (bytes.Length);
			} catch (System.Exception ex) {
				RegenerateThumbnailImage ();
			}

		} else {
			RegenerateThumbnailImage ();
		}

	}

	public static void RegenerateThumbnailImage(){
		Debug.Log("RegenerateThumbnailImage");

		AllocateThumbnails ();

		if(!prefsLoaded){
			LoadFilePaths();
		}

		Texture2D texture = new Texture2D(2048, 1024, TextureFormat.ARGB32, false); // dummy texture so we can load into it
		RenderTexture rt = RenderTexture.GetTemporary(2048, 1024);
		for (int i = 0; i < PhotoIO.saveMap.Count; i++)
		{
			PhotoIO.LoadImage(ref texture, PhotoIO.saveMap[i]);

			Graphics.Blit (texture, rt);
			PhotoIO.InsertThumbnail (rt, PhotoIO.saveMap [i]);
		}

		RenderTexture.ReleaseTemporary (rt);

		SaveThumbnailImage ();

	}

	static void AllocateThumbnails(){
		Debug.Log("Allocating thumbnail texture");
		thumbnails = new Texture2D(4096, 4096, TextureFormat.ARGB32, false);
		thumbnails.wrapMode = TextureWrapMode.Clamp;
	}
}
