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

package com.androidexperiments.sprayscape.unitydriveplugin;

import android.accounts.Account;
import android.accounts.AccountManager;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.res.Configuration;
import android.graphics.PixelFormat;
import android.net.ConnectivityManager;
import android.net.NetworkInfo;
import android.os.AsyncTask;
import android.os.Bundle;
import android.support.v4.app.FragmentActivity;
import android.util.Log;
import android.view.KeyEvent;
import android.view.MotionEvent;
import android.view.Window;

import com.google.android.gms.auth.GoogleAuthException;
import com.google.android.gms.auth.GoogleAuthUtil;
import com.google.android.gms.auth.UserRecoverableAuthException;
import com.google.android.gms.common.AccountPicker;
import com.google.api.client.extensions.android.http.AndroidHttp;
import com.google.api.client.extensions.android.json.AndroidJsonFactory;
import com.google.api.client.googleapis.extensions.android.gms.auth.GoogleAccountCredential;
import com.google.api.client.googleapis.extensions.android.gms.auth.UserRecoverableAuthIOException;
import com.google.api.client.googleapis.json.GoogleJsonResponseException;
import com.google.api.client.http.FileContent;
import com.google.api.client.http.HttpTransport;
import com.google.api.client.json.JsonFactory;
import com.google.api.client.util.ExponentialBackOff;
import com.google.api.services.drive.Drive;
import com.google.api.services.drive.model.File;
import com.google.api.services.drive.model.Permission;
import com.unity3d.player.UnityPlayer;

import java.io.IOException;
import java.util.Arrays;
import java.util.Collections;
import java.util.List;

public class GoogleDriveUnityPlayerActivity extends FragmentActivity {
    protected UnityPlayer mUnityPlayer; // don't change the name of this variable; referenced from native code

    private static final String TAG = "GoogleDriveUPA";
    private static final int REQUEST_CODE_ACCOUNT_SELECTED = 10000;
    private static final int REQUEST_CODE_RECOVER_FROM_PLAY_SERVICES_ERROR = 10001;
    private static final int REQUEST_CODE_RECOVER_FROM_DRIVE_UPLOAD_ERROR = 10002;
    private static final int REQUEST_CODE_RECOVER_FROM_TOKEN_AUTHENTICATE = 10003;

    private static final String CALLBACK_METHOD_DRIVE_ACCOUNT_SELECTED = "DriveAccountSelected";
    private static final String CALLBACK_METHOD_DRIVE_ACCOUNT_TOKEN_OBTAINED = "DriveTokenObtained";
    private static final String CALLBACK_METHOD_DRIVE_ACCOUNT_SELECTION_CANCELED = "DriveAccountSelectionCanceled";
    private static final String CALLBACK_METHOD_DRIVE_IS_READY = "DriveIsReady";
    private static final String CALLBACK_METHOD_DRIVE_FILE_UPLOADED = "DriveFileUploaded";
    private static final String CALLBACK_METHOD_DRIVE_AUTH_FAILED = "DriveAuthFailed";
    private static final String CALLBACK_METHOD_DRIVE_AUTH_CANCELED = "DriveAuthCanceled";
    private static final String CALLBACK_METHOD_DRIVE_PERMISSION_CHANGE_FAILED = "DrivePermissionChangeFailed";
    private static final String CALLBACK_METHOD_NOT_ONLINE = "DriveNotOnline";
    private static final String CALLBACK_METHOD_DRIVE_UPLOAD_FAILED = "DriveUploadFailed";

    private static final String DRIVE_FILE_SCOPE = "https://www.googleapis.com/auth/drive.file";
    private static final String DRIVE_APPFOLDER_SCOPE = "https://www.googleapis.com/auth/drive.appfolder";
    private static final String PLUS_EMAIL_SCOPE = "https://www.googleapis.com/auth/userinfo.email";


    private static final String GOOGLE_ACCOUNT_NAME = "GOOGLE_ACCOUNT_NAME";
    private static final String GOOGLE_ACCOUNT_ID = "GOOGLE_ACCOUNT_ID";

    public static GoogleDriveUnityPlayerActivity activityInstance;

    private GoogleAccountCredential credential;
    private Drive driveService;
    private Account account;
    private String lastDriveFolderName;
    private String lastDriveFileName;
    private String lastLocalPath;
    private String lastCallbackObjectName;

    public GoogleDriveUnityPlayerActivity()    {
        activityInstance = this;
    }

    // Setup activity layout
    @Override protected void onCreate (Bundle savedInstanceState)
    {
        requestWindowFeature(Window.FEATURE_NO_TITLE);
        super.onCreate(savedInstanceState);

        getWindow().setFormat(PixelFormat.RGBX_8888); // <--- This makes xperia play happy

        mUnityPlayer = new UnityPlayer(this);
        setContentView(mUnityPlayer);
        mUnityPlayer.requestFocus();

        credential = GoogleAccountCredential.usingOAuth2(
            getApplicationContext(),
            Arrays.asList(DRIVE_FILE_SCOPE, DRIVE_APPFOLDER_SCOPE, PLUS_EMAIL_SCOPE))
            .setBackOff(new ExponentialBackOff());
    }

    // Quit Unity
    @Override protected void onDestroy ()
    {
        mUnityPlayer.quit();
        super.onDestroy();
    }

    // Pause Unity
    @Override protected void onPause()
    {
        super.onPause();
        mUnityPlayer.pause();
    }

    // Resume Unity
    @Override protected void onResume()
    {
        super.onResume();
        mUnityPlayer.resume();
    }

    // This ensures the layout will be correct.
    @Override public void onConfigurationChanged(Configuration newConfig)
    {
        super.onConfigurationChanged(newConfig);
        mUnityPlayer.configurationChanged(newConfig);
    }

    // Notify Unity of the focus change.
    @Override public void onWindowFocusChanged(boolean hasFocus)
    {
        super.onWindowFocusChanged(hasFocus);
        mUnityPlayer.windowFocusChanged(hasFocus);
    }

    // For some reason the multiple keyevent type is not supported by the ndk.
    // Force event injection by overriding dispatchKeyEvent().
    @Override public boolean dispatchKeyEvent(KeyEvent event)
    {
        if (event.getAction() == KeyEvent.ACTION_MULTIPLE)
            return mUnityPlayer.injectEvent(event);
        return super.dispatchKeyEvent(event);
    }

    // Pass any events not handled by (unfocused) views straight to UnityPlayer
    @Override public boolean onKeyUp(int keyCode, KeyEvent event)     { return mUnityPlayer.injectEvent(event); }
    @Override public boolean onKeyDown(int keyCode, KeyEvent event)   { return mUnityPlayer.injectEvent(event); }
    @Override public boolean onTouchEvent(MotionEvent event)          { return mUnityPlayer.injectEvent(event); }
    /*API12*/ public boolean onGenericMotionEvent(MotionEvent event)  { return mUnityPlayer.injectEvent(event); }

    public boolean isDeviceOnline() {
        ConnectivityManager connMgr = (ConnectivityManager) getSystemService(Context.CONNECTIVITY_SERVICE);
        NetworkInfo networkInfo = connMgr.getActiveNetworkInfo();
        return (networkInfo != null && networkInfo.isConnected());
    }

    private Account getLastUsedAccount() {
        String accountName = getPreferences(MODE_PRIVATE).getString(GOOGLE_ACCOUNT_NAME, null);
        if (accountName == null) {
            account = null;
            return null;
        }

        account = getAccountByName(accountName);
        return account;
    }

    private Account getAccountByName(String name) {
        for (Account a: AccountManager.get(this).getAccountsByType("com.google")) {
            if (a.name.equals(name)) {
                return a;
            }
        }
        return null;
    }

    private void clearAccount()    {
        account = null;
        driveService = null; // this also needs to be cleared out!
        SharedPreferences prefs = getPreferences(MODE_PRIVATE);
        SharedPreferences.Editor e = prefs.edit();
        e.remove(GOOGLE_ACCOUNT_NAME);
        e.apply();
    }

    private Drive initDriveServiceFromAccount(Account account, String callbackObjectName){
        Log.i(TAG, "initDriveServiceFromAccount");
        credential.setSelectedAccountName(account.name);

        // Send account to unity app
        UnityPlayer.UnitySendMessage(callbackObjectName, CALLBACK_METHOD_DRIVE_ACCOUNT_SELECTED, account.name);
        HttpTransport transport = AndroidHttp.newCompatibleTransport();
        JsonFactory jsonFactory = AndroidJsonFactory.getDefaultInstance();
        driveService = new com.google.api.services.drive.Drive.Builder(transport, jsonFactory, credential)
                .setApplicationName("Sprayscape")
                .build();

        UnityPlayer.UnitySendMessage(callbackObjectName, CALLBACK_METHOD_DRIVE_IS_READY, "");


        return driveService;
    }

    @Override
    protected void onActivityResult(final int requestCode, final int resultCode, final Intent data) {
        Log.i(TAG, "onActivityResult(" + requestCode + ", " + resultCode + ", " + data + ")");
        UnityPlayer.UnitySendMessage("OnActivityResultListener", "OnActivityResult", requestCode + "," + resultCode + "," + data);
        switch (requestCode) {
            case REQUEST_CODE_ACCOUNT_SELECTED: {
                if (resultCode == RESULT_OK) {
                    final String accountName = data.getStringExtra(AccountManager.KEY_ACCOUNT_NAME);
                    String accountType = data.getStringExtra(AccountManager.KEY_ACCOUNT_TYPE);
                    SharedPreferences prefs = getPreferences(MODE_PRIVATE);

                    final SharedPreferences.Editor e = prefs.edit();
                    e.putString(GOOGLE_ACCOUNT_NAME, accountName);
                    e.apply();

                    Log.i(TAG, "user selected account: " + accountName + " type: " + accountType + " saved to shared preferences");
                    account = new Account(accountName, accountType);
                    // TODO: maybe don't assume the user was trying to upload?
                    // try uploading again
                    //uploadFile(lastDriveFolderName, lastDriveFileName, lastLocalPath, lastCallbackObjectName);

                    driveService = initDriveServiceFromAccount(account, lastCallbackObjectName);

                } else {
                    // user cancel the account picker without selecting....
                    UnityPlayer.UnitySendMessage(lastCallbackObjectName, CALLBACK_METHOD_DRIVE_ACCOUNT_SELECTION_CANCELED, "" + resultCode);
                }
                break;
            }
            case REQUEST_CODE_RECOVER_FROM_TOKEN_AUTHENTICATE: {
//                if(resultCode == RESULT_OK){
                    fetchAuthToken(lastCallbackObjectName);
//                } else {
//                    clearAccount();
//                    UnityPlayer.UnitySendMessage(lastCallbackObjectName, CALLBACK_METHOD_DRIVE_AUTH_CANCELED, "" + resultCode);
//                }
                break;
            }
            case REQUEST_CODE_RECOVER_FROM_DRIVE_UPLOAD_ERROR: {
                if (resultCode == RESULT_OK) {
                    // user fixed the authentication problem try upload again
                    uploadFile(lastDriveFolderName, lastDriveFileName, lastLocalPath, lastCallbackObjectName);
                } else {
                    // this happens when the user deny's the authorization
                    // we will clear the auth data in-case they wanted to switch accounts on the next try
                    clearAccount();
                    UnityPlayer.UnitySendMessage(lastCallbackObjectName, CALLBACK_METHOD_DRIVE_AUTH_CANCELED, "" + resultCode);
                }
                break;
            }
            default:
                super.onActivityResult(requestCode, resultCode, data);
        }
    }

    private class UploadFileToDrive extends AsyncTask<Void, Void, String> {

        private String driveFolderName;
        private String driveFileName;
        private String localPath;
        private String callbackObjectName;

        public UploadFileToDrive(String driveFolderName, String driveFileName, String localPath, String callbackObjectName) {
            this.driveFolderName = driveFolderName;
            this.driveFileName = driveFileName;
            this.localPath = localPath;
            this.callbackObjectName = callbackObjectName;
        }

        @Override
        protected String doInBackground(Void... params) {
            try {
                // first make sure we are online
                if (!isDeviceOnline()) {
                    UnityPlayer.UnitySendMessage(this.callbackObjectName, CALLBACK_METHOD_NOT_ONLINE, "no connection");
                    // this will trigger the generic drive failure
                    throw new Exception("Not online");
                }

                File topFolder = ensureDriveFolderExists(driveFolderName);

                //ensureFolderPermissions(topFolder);

                File imageFile = new File();
                imageFile.setName(driveFileName);
                imageFile.setMimeType("image/jpeg");
                imageFile.setParents(Collections.singletonList(topFolder.getId()));
                java.io.File filePath = new java.io.File(localPath);
                FileContent mediaContent = new FileContent("image/jpeg", filePath);
                File file = driveService.files().create(imageFile, mediaContent)
                        .setFields("id")
                        .execute();

                Log.i(TAG, "Drive File Uploaded: '" + driveFolderName + "/" + driveFileName + "': " + file.getId());

                ensureFilePermissions(file);

                return file.getId();
            } catch (UserRecoverableAuthIOException ex) {
                ex.getIntent();
                startActivityForResult(ex.getIntent(), REQUEST_CODE_RECOVER_FROM_DRIVE_UPLOAD_ERROR);
                return null;
            } catch (Exception ex) {
                Log.e(TAG, "Failed to upload to drive", ex);
                UnityPlayer.UnitySendMessage(this.callbackObjectName, CALLBACK_METHOD_DRIVE_UPLOAD_FAILED, ex.toString());
                return null;
            }
        }

        private File ensureDriveFolderExists(String driveFolderName)throws IOException {
            List<File> files = driveService.files().list().setQ("mimeType='application/vnd.google-apps.folder' and name = '" + driveFolderName + "' and trashed=false").execute().getFiles();
            if (files.size() == 1) {
                File f = files.get(0);
                Log.i(TAG, "Drive Folder: '" + driveFolderName + "' already exists: " + f.getId());
                return f;
            }
            // else folder doesn't exist yet
            Log.i(TAG, "Drive Folder: '" + driveFolderName + "' does not exist yet, creating now");

            File f = new File();
            f.setName(driveFolderName);
            f.setMimeType("application/vnd.google-apps.folder");

            f = driveService.files().create(f).setFields("id").execute();
            Log.i(TAG, "Drive Folder: '" + driveFolderName + "' created: " + f.getId());
            return f;
        }

        private void ensureFolderPermissions(File folder)throws IOException {
            for (Permission p:  driveService.permissions().list(folder.getId()).execute().getPermissions()) {
                if (p.getType().equals("anyone") && p.getRole().equals("reader")) {
                    Log.i(TAG, "anyone/reader permission already found on folder");
                    return;
                }
            }
            // reader permission not found, adding...
            try {
                Permission p = new Permission();
                p.setType("anyone");
                p.setRole("reader");
                p = driveService.permissions().create(folder.getId(), p).execute();
                Log.i(TAG, "added permission to top-level folder: " + p.toPrettyString());
            } catch (GoogleJsonResponseException ex) {
                // HACK: we specifically look for a 403 on the permission change, this our signal that the account is probably a google.com account
                // we clear out the account selection here as well to force account selection again since that account will likely never work
                int errorCode = ex.getDetails().getCode();
                Log.w(TAG, "GoogleJsonResponseException.getDetails().getCode() == " + errorCode);
                if (errorCode == 403) {
                    clearAccount();
                    UnityPlayer.UnitySendMessage(this.callbackObjectName, CALLBACK_METHOD_DRIVE_PERMISSION_CHANGE_FAILED, ex.toString());
                }
                throw ex;
            }
        }


        private void ensureFilePermissions(File file)throws IOException {
            for (Permission p:  driveService.permissions().list(file.getId()).execute().getPermissions()) {
                if (p.getType().equals("anyone") && p.getRole().equals("reader")) {
                    Log.i(TAG, "anyone/reader permission already found on folder");
                    return;
                }
            }
            // reader permission not found, adding...
            try {
                Permission p = new Permission();
                p.setType("anyone");
                p.setRole("reader");
                p = driveService.permissions().create(file.getId(), p).execute();
                Log.i(TAG, "added permission to file: " + p.toPrettyString());
            } catch (GoogleJsonResponseException ex) {
                // HACK: we specifically look for a 403 on the permission change, this our signal that the account is probably a google.com account
                // we clear out the account selection here as well to force account selection again since that account will likely never work
                int errorCode = ex.getDetails().getCode();
                Log.w(TAG, "GoogleJsonResponseException.getDetails().getCode() == " + errorCode);
                if (errorCode == 403) {
                    clearAccount();
                    UnityPlayer.UnitySendMessage(this.callbackObjectName, CALLBACK_METHOD_DRIVE_PERMISSION_CHANGE_FAILED, ex.toString());
                }
                throw ex;
            }
        }


        @Override
        protected void onPostExecute(String fileId) {
            // force on the ui thread just in case...
            if (fileId != null) {
                UnityPlayer.UnitySendMessage(this.callbackObjectName, CALLBACK_METHOD_DRIVE_FILE_UPLOADED, fileId);
            }
        }
    };

    public boolean checkAccountPermissions(String callbackObjectName) throws Exception {
        lastCallbackObjectName = callbackObjectName;

        if (account == null) {
            account = getLastUsedAccount();
        }

        // Check account has been setup already
        if (account == null) {
            // the user account has not previously been selected yet, trigger the account picker
            // only allow google accounts...
            Intent intent = AccountPicker.newChooseAccountIntent(null, null, new String[]{"com.google"}, false, null, null, null, null);
            startActivityForResult(intent, REQUEST_CODE_ACCOUNT_SELECTED);
            return false;
        }

        // Check if drive service is already setup, or call async function to set it up
        else if (driveService == null) {
            driveService = initDriveServiceFromAccount(account, callbackObjectName);
        }

        // Everything is ready, call DriveIsReady in unity
        else {
            UnityPlayer.UnitySendMessage(callbackObjectName, CALLBACK_METHOD_DRIVE_IS_READY, "");
        }
        return true;
    }

    public boolean fetchAuthToken(final String optObjectName){
        Log.d(TAG,"fetchAuthToken()");
        AsyncTask t = new AsyncTask<Void, Void,Void>() {
            private String obtained_token;
            @Override
            protected Void doInBackground(Void... params) {
                // Obtain google auth access token
                try {
//                    String mScope = "oauth2:" + PLUS_EMAIL_SCOPE;
//                    obtained_token = GoogleAuthUtil.getToken(getApplicationContext(), account, mScope);
                    obtained_token = credential.getToken();
                    Log.d(TAG,"fetchAuthToken() token obtained: "+obtained_token);

                } catch (UserRecoverableAuthException userRecoverableException) {
                    Log.e(TAG, "UserRecoverableException " + userRecoverableException.getMessage());
                    lastCallbackObjectName = optObjectName;
//                    userRecoverableException.getIntent();
                    startActivityForResult(userRecoverableException.getIntent(), REQUEST_CODE_RECOVER_FROM_TOKEN_AUTHENTICATE);
                } catch (GoogleAuthException | IOException e) {
                    e.printStackTrace();
                    Log.e(TAG, "Exception " + e.getMessage());

                    clearAccount();
                    UnityPlayer.UnitySendMessage(lastCallbackObjectName, CALLBACK_METHOD_DRIVE_AUTH_FAILED, "" + e.getMessage());
                }
                return null;
            }

            @Override
            protected void onPostExecute(Void o) {
                if (optObjectName != null && obtained_token != null)
                    UnityPlayer.UnitySendMessage(optObjectName, CALLBACK_METHOD_DRIVE_ACCOUNT_TOKEN_OBTAINED, obtained_token);

                super.onPostExecute(o);
            }
        }.execute();
        return true;
    }

    public boolean checkFileId(String fileId){
        Log.i(TAG, "checkFileId(\"" + fileId + "\")");

        if(account == null) return false;

        try {
            File f = driveService.files().get(fileId).execute();
            return f != null;
        } catch (IOException e) {
            e.printStackTrace();
        }
        return false;
    }

    public boolean uploadFile(String driveFolderName, String driveFileName, String localPath, String callbackObjectName) {
        Log.i(TAG, "uploadFile(\"" + driveFileName +"\", \"" + driveFileName + "\", \"" + localPath + "\",\"" + callbackObjectName + "\")");

        // HACK: used if the authentication failed and the user needs to try on onActivityResult (UGH)
        lastDriveFolderName = driveFolderName;
        lastDriveFileName = driveFileName;
        lastLocalPath = localPath;
        lastCallbackObjectName = callbackObjectName;

        if(account == null) return false;

        UploadFileToDrive task = new UploadFileToDrive(driveFolderName, driveFileName, localPath, callbackObjectName);
        task.execute();

        return true;
    }
}
