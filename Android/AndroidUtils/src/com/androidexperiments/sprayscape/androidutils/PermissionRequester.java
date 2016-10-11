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

package com.androidexperiments.sprayscape.androidutils;

import android.os.Build;
import android.annotation.SuppressLint;
import android.app.Activity;
import android.app.AlertDialog;
import android.app.Fragment;
import android.app.FragmentManager;
import android.app.FragmentTransaction;
import android.util.Log;
import android.content.DialogInterface;
import android.content.DialogInterface.OnClickListener;
import android.content.DialogInterface.OnDismissListener;
import android.content.pm.PackageManager;

import com.unity3d.player.UnityPlayer;

import android.support.v13.app.*;
import android.support.v4.app.ActivityCompat;

public class PermissionRequester {
	private final static String TAG = "PermissionRequester";
	private final static String DEFAULT_UNITY_CALLBACK_GAMEOBJECT_NAME = "PermissionCallbackReceiver";
	private final static String DEFAULT_UNITY_CALLBACK_METHOD_NAME = "PermissionCallback";
	
	private static void SendMessage(final String objectName, final String methodName) {
		try {
			UnityPlayer.UnitySendMessage(objectName, methodName, "");
		} catch (Exception err) {
			Log.e(TAG, "Failed to send message to untiy method: " + objectName + "." + methodName + "(string)", err);
		}
	}

	private static void SendPermissionResult(final String permission, final boolean value, final String objectName, final String methodName) {
		try {
			UnityPlayer.UnitySendMessage(objectName, methodName, permission + "," + value);
		} catch (Exception err) {
			Log.e(TAG, "Failed to send message to untiy method: " + objectName + "." + methodName + "(string)", err);
		}
	}
	
	public static boolean hasPermission(final String permission) {
		final Activity currentActivity = UnityPlayer.currentActivity;
		int res = ActivityCompat.checkSelfPermission(currentActivity, permission);
		return res == PackageManager.PERMISSION_GRANTED;
	}
	
	public static boolean shouldShowRequestPermissionRationale(final String permission) {
		final Activity currentActivity = UnityPlayer.currentActivity;
		return ActivityCompat.shouldShowRequestPermissionRationale(currentActivity, permission);
	}
	
	public static void showPermissionRationaleDialog(final String permission, final String title, final String message) {
		showPermissionRationaleDialog(permission, title, message, DEFAULT_UNITY_CALLBACK_GAMEOBJECT_NAME, DEFAULT_UNITY_CALLBACK_METHOD_NAME);
	}
	
	public static void showPermissionRationaleDialog(final String permission, final String title, final String message, final String objectName, final String methodName) {
		final Activity currentActivity = UnityPlayer.currentActivity;
		// have to run the dialog in the UI thread:
		currentActivity.runOnUiThread(new Runnable() {
			@Override
			public void run() {
				new AlertDialog.Builder(currentActivity)
				.setTitle(title)
				.setMessage(message)
				.setPositiveButton(android.R.string.ok, new OnClickListener() {
					@Override
					public void onClick(DialogInterface dialog, int which) {} // no-op, let OnDismissListener handle it 
				})
				.setOnDismissListener(new OnDismissListener() {
					@Override
					public void onDismiss(DialogInterface dialog) {
						requestPermission(permission, objectName, methodName);
					}
				})
				.setCancelable(false)
				.show();
			}
		});
	}
	
	public static void showDialog(final String title, final String message, final String objectName, final String methodName) {
		final Activity currentActivity = UnityPlayer.currentActivity;
		// have to run the dialog in the UI thread:
		currentActivity.runOnUiThread(new Runnable() {
			@Override
			public void run() {
				new AlertDialog.Builder(currentActivity)
				.setTitle(title)
				.setMessage(message)
				.setPositiveButton(android.R.string.ok, new OnClickListener() {
					@Override
					public void onClick(DialogInterface dialog, int which) {} // no-op, let OnDismissListener handle it 
				})
				.setOnDismissListener(new OnDismissListener() {
					@Override
					public void onDismiss(DialogInterface dialog) {
						SendMessage(objectName, methodName);
					}
				})
				.setCancelable(false)
				.show();
			}
		});
	}
	
	public static void requestPermission(final String permission) {
		requestPermission(permission, DEFAULT_UNITY_CALLBACK_GAMEOBJECT_NAME, DEFAULT_UNITY_CALLBACK_METHOD_NAME);
	}

	public static void requestPermission(final String permission, final String objectName, final String methodName) {
		Log.i(TAG, "requestPermission() called for permission: " + permission);
		
		final Activity currentActivity = UnityPlayer.currentActivity;
		boolean granted = ActivityCompat.checkSelfPermission(currentActivity, permission) == PackageManager.PERMISSION_GRANTED;
		
		if (granted) {
			Log.i(TAG, "Permission already granted: " + permission);
			SendPermissionResult(permission, true, objectName, methodName);
			return;
		}
		
		try {
			// use a fragment here because we wouldn't be able to capture onRequestPermissionsResult() on the main activity unless we sub-classed it...
			// final values so the fragment inner-class can access them
			final FragmentManager fragmentManager = currentActivity.getFragmentManager();
			final Fragment request = new Fragment() {

				@Override
				public void onStart() {
					super.onStart();
					Log.i(TAG, "Permission fragment onStart()");
					FragmentCompat.requestPermissions(this,
							new String[] { permission }, 0);
				}

				@SuppressLint("Override")
				@SuppressWarnings("unused")
				public void onRequestPermissionsResult(int requestCode, String[] permissions, int[] grantResults) {
					Log.i(TAG, "onRequestPermissionsResult(" + requestCode + ", " + permissions.toString() + ", " + grantResults.toString() + ")");

					if (grantResults.length > 0) {
						for (int i=0 ; i < grantResults.length; i++) {
							if (grantResults[i] == PackageManager.PERMISSION_GRANTED) {
								Log.i(TAG, "Permission granted: " + permissions[i]);
								SendPermissionResult(permissions[i], true, objectName, methodName);
							} else {
								Log.i(TAG, "Permission denied: " + permissions[i]);
								SendPermissionResult(permissions[i], false, objectName, methodName);
							}
						}
					} else {
						Log.i(TAG, "Permission denied: " + permission);
						SendPermissionResult(permission, false, objectName, methodName);
					}

					FragmentTransaction fragmentTransaction = fragmentManager.beginTransaction();
					fragmentTransaction.remove(this);
					fragmentTransaction.commit();
				}
			};

			FragmentTransaction fragmentTransaction = fragmentManager.beginTransaction();
			fragmentTransaction.add(0, request);
			fragmentTransaction.commit();
		} catch (Exception error) {
			Log.e(TAG, "Permission request failed: '" + permission + "'", error);
			SendPermissionResult(permission, false, objectName, methodName);
		}
	}
}
