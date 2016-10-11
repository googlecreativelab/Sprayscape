/*
  Copyright 2014 Google Inc. All rights reserved.

  Licensed under the Apache License, Version 2.0 (the "License");
  you may not use this file except in compliance with the License.
  You may obtain a copy of the License at

      http://www.apache.org/licenses/LICENSE-2.0

  Unless required by applicable law or agreed to in writing, software
  distributed under the License is distributed on an "AS IS" BASIS,
  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  See the License for the specific language governing permissions and
  limitations under the License.
*/

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/*
  GoogleAnalyticsAndroidV3 handles building hits using the Android SDK.
  Developers should call the methods in GoogleAnalyticsV3, which will call the
  appropriate methods in this class if the application is built for Android.
*/
public class GoogleAnalyticsAndroidV3 : IDisposable {
#if UNITY_ANDROID && !UNITY_EDITOR
  private string trackingCode;
  private string appVersion;
  private string appName;
  private string bundleIdentifier;
  private int dispatchPeriod;
  private int sampleFrequency;
  private GoogleAnalyticsV3.DebugMode logLevel;
  private bool anonymizeIP;
  private bool dryRun;
  private int sessionTimeout;
  private AndroidJavaObject tracker;
  private AndroidJavaObject logger;
  private AndroidJavaObject currentActivityObject;
  private AndroidJavaObject googleAnalyticsSingleton;
  private AndroidJavaObject gaServiceManagerSingleton;
  private AndroidJavaClass analyticsTrackingFields;
  private bool startSessionOnNextHit = false;
  private bool endSessionOnNextHit = false;

  internal void InitializeTracker() {
    Debug.Log("Initializing Google Analytics Android Tracker.");

    analyticsTrackingFields = new AndroidJavaClass(
        "com.google.analytics.tracking.android.Fields");
    using (AndroidJavaObject googleAnalyticsClass = new AndroidJavaClass(
        "com.google.analytics.tracking.android.GoogleAnalytics"))
    using (AndroidJavaClass googleAnalyticsServiceManagerClass =
        new AndroidJavaClass(
        "com.google.analytics.tracking.android.GAServiceManager"))
    using (AndroidJavaClass jc = new AndroidJavaClass(
        "com.unity3d.player.UnityPlayer")) {
      currentActivityObject = jc.GetStatic<AndroidJavaObject>(
          "currentActivity");
      googleAnalyticsSingleton = googleAnalyticsClass.
          CallStatic<AndroidJavaObject>("getInstance", currentActivityObject);
      gaServiceManagerSingleton = googleAnalyticsServiceManagerClass.
          CallStatic<AndroidJavaObject>("getInstance");

      gaServiceManagerSingleton.Call(
          "setLocalDispatchPeriod", dispatchPeriod);

      tracker = googleAnalyticsSingleton.Call<AndroidJavaObject>(
          "getTracker", trackingCode);

      SetTrackerVal(Fields.SAMPLE_RATE, sampleFrequency.ToString());
      SetTrackerVal(Fields.APP_NAME, appName);
      SetTrackerVal(Fields.APP_ID, bundleIdentifier);
      SetTrackerVal(Fields.APP_VERSION, appVersion);

      if (anonymizeIP) {
        SetTrackerVal(Fields.ANONYMIZE_IP, "1");
      }
      googleAnalyticsSingleton.Call("setDryRun", dryRun);

      SetLogLevel(logLevel);
    }
  }

  internal void SetTrackerVal(Field fieldName, object value) {
    object[] args = new object[] { fieldName.ToString(), value };
    tracker.Call(GoogleAnalyticsV3.SET, args);
  }

  public void SetSampleFrequency(int sampleFrequency) {
    this.sampleFrequency = sampleFrequency;
  }

  private void SetLogLevel(GoogleAnalyticsV3.DebugMode logLevel) {
    using (logger = googleAnalyticsSingleton.
        Call<AndroidJavaObject>("getLogger"))
    using (AndroidJavaClass log = new AndroidJavaClass(
        "com.google.analytics.tracking.android.Logger$LogLevel")) {
      switch(logLevel)
      {
        case GoogleAnalyticsV3.DebugMode.ERROR:
          using (AndroidJavaObject level =
              log.GetStatic<AndroidJavaObject>("ERROR")){
            logger.Call("setLogLevel", level);
          }
          break;
        case GoogleAnalyticsV3.DebugMode.VERBOSE:
          using (AndroidJavaObject level =
              log.GetStatic<AndroidJavaObject>("VERBOSE")){
            logger.Call("setLogLevel", level);
          }
          break;
        case GoogleAnalyticsV3.DebugMode.INFO:
          using (AndroidJavaObject level =
              log.GetStatic<AndroidJavaObject>("INFO")){
            logger.Call("setLogLevel", level);
          }
          break;
        default:
          using (AndroidJavaObject level =
              log.GetStatic<AndroidJavaObject>("WARNING")){
            logger.Call("setLogLevel", level);
          }
          break;
      }
    }
  }

  private void SetSessionOnBuilder(AndroidJavaObject hitBuilder) {
    if (startSessionOnNextHit) {
      object[] args = {Fields.SESSION_CONTROL.ToString(), "start"};
      hitBuilder.Call<AndroidJavaObject>("set", args);
      startSessionOnNextHit = false;
    } else if (endSessionOnNextHit) {
      object[] args = {Fields.SESSION_CONTROL.ToString(), "end"};
      hitBuilder.Call<AndroidJavaObject>("set", args);
      endSessionOnNextHit = false;
    }
  }

  private AndroidJavaObject BuildMap(string methodName) {
    using (AndroidJavaClass mapBuilder = new AndroidJavaClass(
        "com.google.analytics.tracking.android.MapBuilder"))
    using (AndroidJavaObject hitMapBuilder = mapBuilder.
        CallStatic<AndroidJavaObject>(methodName)) {
      SetSessionOnBuilder(hitMapBuilder);
      return hitMapBuilder.Call<AndroidJavaObject>("build");
    }
  }

  private AndroidJavaObject BuildMap (string methodName, object[] args) {
    using (AndroidJavaClass mapBuilder = new AndroidJavaClass(
        "com.google.analytics.tracking.android.MapBuilder"))
    using (AndroidJavaObject hitMapBuilder = mapBuilder.
        CallStatic<AndroidJavaObject>(methodName, args)) {
      SetSessionOnBuilder(hitMapBuilder);
      return hitMapBuilder.Call<AndroidJavaObject>("build");
    }
  }

  private AndroidJavaObject BuildMap(string methodName,
      Dictionary<AndroidJavaObject,string> parameters) {
    return BuildMap(methodName, null, parameters);
  }

  private AndroidJavaObject BuildMap(string methodName, object[] simpleArgs,
      Dictionary<AndroidJavaObject,string> parameters) {
    using (AndroidJavaObject hashMap =
        new AndroidJavaObject("java.util.HashMap"))
    using (AndroidJavaClass mapBuilder = new AndroidJavaClass(
        "com.google.analytics.tracking.android.MapBuilder")) {
      IntPtr putMethod = AndroidJNIHelper.GetMethodID(
          hashMap.GetRawClass(), "put",
          "(Ljava/lang/Object;Ljava/lang/Object;)Ljava/lang/Object;");
      object[] args = new object[2];
      foreach (KeyValuePair<AndroidJavaObject, string> kvp in parameters) {
        using (AndroidJavaObject k = kvp.Key) {
          using (AndroidJavaObject v = new AndroidJavaObject(
              "java.lang.String", kvp.Value)) {
            args [0] = k;
            args [1] = v;
            AndroidJNI.CallObjectMethod(hashMap.GetRawObject(),
                putMethod, AndroidJNIHelper.CreateJNIArgArray(args));
          }
        }
      }
      if (simpleArgs != null) {
        using (AndroidJavaObject hitMapBuilder =
            mapBuilder.CallStatic<AndroidJavaObject>(methodName, simpleArgs)) {
          hitMapBuilder.Call<AndroidJavaObject>(GoogleAnalyticsV3.SET_ALL,
              hashMap);
          SetSessionOnBuilder(hitMapBuilder);
          return hitMapBuilder.Call<AndroidJavaObject>("build");
        }
      } else {
        using (AndroidJavaObject hitMapBuilder =
            mapBuilder.CallStatic<AndroidJavaObject>(methodName)) {
          hitMapBuilder.Call<AndroidJavaObject>(GoogleAnalyticsV3.SET_ALL,
              hashMap);
          SetSessionOnBuilder(hitMapBuilder);
          return hitMapBuilder.Call<AndroidJavaObject>("build");
        }
      }
    }
  }

  private Dictionary<AndroidJavaObject, string>
      AddCustomVariablesAndCampaignParameters<T>(HitBuilder<T> builder) {
    Dictionary<AndroidJavaObject, string> parameters =
        new Dictionary<AndroidJavaObject, string>();
    AndroidJavaObject fieldName;
    foreach (KeyValuePair<int, string> entry in builder.GetCustomDimensions()) {
      fieldName = analyticsTrackingFields.CallStatic<AndroidJavaObject>(
          "customDimension", entry.Key);
      parameters.Add(fieldName, entry.Value);
    }
    foreach (KeyValuePair<int, string> entry in builder.GetCustomMetrics()) {
      fieldName = analyticsTrackingFields.CallStatic<AndroidJavaObject>(
          "customMetric", entry.Key);
      parameters.Add(fieldName, entry.Value);
    }

    if (parameters.Keys.Count > 0) {
      if (GoogleAnalyticsV3.belowThreshold(logLevel, GoogleAnalyticsV3.DebugMode.VERBOSE)) {
        Debug.Log("Added custom variables to hit.");
      }
    }

    if (!String.IsNullOrEmpty(builder.GetCampaignSource())) {
      fieldName = analyticsTrackingFields.
          GetStatic<AndroidJavaObject>("CAMPAIGN_SOURCE");
      parameters.Add(fieldName, builder.GetCampaignSource());
      fieldName = analyticsTrackingFields.
          GetStatic<AndroidJavaObject>("CAMPAIGN_MEDIUM");
      parameters.Add(fieldName, builder.GetCampaignMedium());
      fieldName = analyticsTrackingFields.
          GetStatic<AndroidJavaObject>("CAMPAIGN_NAME");
      parameters.Add(fieldName, builder.GetCampaignName());
      fieldName = analyticsTrackingFields.
          GetStatic<AndroidJavaObject>("CAMPAIGN_CONTENT");
      parameters.Add(fieldName, builder.GetCampaignContent());
      fieldName = analyticsTrackingFields.
          GetStatic<AndroidJavaObject>("CAMPAIGN_KEYWORD");
      parameters.Add(fieldName, builder.GetCampaignKeyword());
      fieldName = analyticsTrackingFields.
          GetStatic<AndroidJavaObject>("CAMPAIGN_ID");
      parameters.Add(fieldName, builder.GetCampaignID());
      fieldName = analyticsTrackingFields.
          GetStatic<AndroidJavaObject>("GCLID");
      parameters.Add(fieldName, builder.GetGclid());
      fieldName = analyticsTrackingFields.
          GetStatic<AndroidJavaObject>("DCLID");
      parameters.Add(fieldName, builder.GetDclid());
      if (GoogleAnalyticsV3.belowThreshold(logLevel, GoogleAnalyticsV3.DebugMode.VERBOSE)) {
        Debug.Log("Added campaign parameters to hit.");
      }
    }

    if (parameters.Keys.Count > 0) {
      return parameters;
    }
    return null;
  }

  internal void StartSession() {
    startSessionOnNextHit = true;
  }

  internal void StopSession() {
    endSessionOnNextHit = true;
  }

  public void SetOptOut(bool optOut) {
    googleAnalyticsSingleton.Call("setAppOptOut", optOut);
  }

  internal void LogScreen (AppViewHitBuilder builder) {
    using (AndroidJavaObject screenName = analyticsTrackingFields.
        GetStatic<AndroidJavaObject>("SCREEN_NAME")) {
      object[] args = new object[] { screenName, builder.GetScreenName() };
      tracker.Call (GoogleAnalyticsV3.SET, args);
    }

    Dictionary<AndroidJavaObject, string> parameters =
        AddCustomVariablesAndCampaignParameters(builder);
    if (parameters != null) {
      object map = BuildMap(GoogleAnalyticsV3.APP_VIEW, parameters);
      tracker.Call(GoogleAnalyticsV3.SEND, map);
    } else {
      object[] args = new object[] { BuildMap(GoogleAnalyticsV3.APP_VIEW) };
      tracker.Call(GoogleAnalyticsV3.SEND, args);
    }
  }

  internal void LogEvent(EventHitBuilder builder) {
    using (AndroidJavaObject valueObj = new AndroidJavaObject (
        "java.lang.Long", builder.GetEventValue())) {
      object[] args = new object[4];
      args[0] = builder.GetEventCategory();
      args[1] = builder.GetEventAction();
      args[2] = builder.GetEventLabel();
      args[3] = valueObj;

      object map;
      Dictionary<AndroidJavaObject, string> parameters =
          AddCustomVariablesAndCampaignParameters(builder);
      if (parameters != null) {
        map = BuildMap(GoogleAnalyticsV3.EVENT_HIT, args, parameters);
      } else {
        map = BuildMap(GoogleAnalyticsV3.EVENT_HIT, args);
      }
      tracker.Call (GoogleAnalyticsV3.SEND, map);
    }
  }

  internal void LogTransaction(TransactionHitBuilder builder) {
    AndroidJavaObject[] valueObj = new AndroidJavaObject[3];
    valueObj[0] = new AndroidJavaObject("java.lang.Double",
        builder.GetRevenue());
    valueObj[1] = new AndroidJavaObject("java.lang.Double",
        builder.GetTax());
    valueObj[2] = new AndroidJavaObject("java.lang.Double",
        builder.GetShipping());
    object[] args  = new object[6];
    args[0] = builder.GetTransactionID();
    args[1] = builder.GetAffiliation();
    args[2] = valueObj[0];
    args[3] = valueObj[1];
    args[4] = valueObj[2];
    if (builder.GetCurrencyCode() == null) {
        args[5] = GoogleAnalyticsV3.currencySymbol;
    }
    else {
        args[5] = builder.GetCurrencyCode();
    }
    object map;
    Dictionary<AndroidJavaObject, string> parameters =
        AddCustomVariablesAndCampaignParameters(builder);
    if (parameters != null){
      map = BuildMap(GoogleAnalyticsV3.TRANSACTION_HIT, args, parameters);
    } else {
      map = BuildMap(GoogleAnalyticsV3.TRANSACTION_HIT, args);
    }
    tracker.Call(GoogleAnalyticsV3.SEND, map);
  }

  internal void LogItem(ItemHitBuilder builder) {
    object[] args;
    if (builder.GetCurrencyCode() != null) {
      args = new object[7];
      // TODO: Validate currency code
      args[6] = builder.GetCurrencyCode();
    } else {
      args = new object[6];
    }
    args[0] = builder.GetTransactionID();
    args[1] = builder.GetName();
    args[2] = builder.GetSKU();
    args[3] = builder.GetCategory();
    args[4] = new AndroidJavaObject("java.lang.Double", builder.GetPrice());
    args[5] = new AndroidJavaObject("java.lang.Long", builder.GetQuantity());

    object map;
    Dictionary<AndroidJavaObject, string> parameters =
        AddCustomVariablesAndCampaignParameters(builder);
    if (parameters != null) {
      map = BuildMap(GoogleAnalyticsV3.ITEM_HIT, args, parameters);
    } else {
      map = BuildMap(GoogleAnalyticsV3.ITEM_HIT, args);
    }
    tracker.Call(GoogleAnalyticsV3.SEND, map);
  }

  public void LogException(ExceptionHitBuilder builder) {
      object[] args = new object[2];
      args[0] = builder.GetExceptionDescription();
      args[1] = new AndroidJavaObject("java.lang.Boolean", builder.IsFatal());
      object map;
      Dictionary<AndroidJavaObject, string> parameters =
          AddCustomVariablesAndCampaignParameters(builder);
      if (parameters != null) {
        map = BuildMap(GoogleAnalyticsV3.EXCEPTION_HIT, args, parameters);
      } else {
        map = BuildMap(GoogleAnalyticsV3.EXCEPTION_HIT, args);
      }
      tracker.Call(GoogleAnalyticsV3.SEND, map);
  }

  public void DispatchHits() {
    gaServiceManagerSingleton.Call("dispatchLocalHits");
  }

  public void LogSocial(SocialHitBuilder builder) {
    object[] args = new object[3];
    args[0] = builder.GetSocialNetwork();
    args[1] = builder.GetSocialAction();
    args[2] = builder.GetSocialTarget();

    object map;
    Dictionary<AndroidJavaObject, string> parameters =
        AddCustomVariablesAndCampaignParameters(builder);
    if (parameters != null) {
      map = BuildMap(GoogleAnalyticsV3.SOCIAL_HIT, args, parameters);
    } else {
      map = BuildMap(GoogleAnalyticsV3.SOCIAL_HIT, args);
    }
    tracker.Call(GoogleAnalyticsV3.SEND, map);
  }

  public void LogTiming(TimingHitBuilder builder) {
    using (AndroidJavaObject valueObj =
        new AndroidJavaObject("java.lang.Long", builder.GetTimingInterval())) {
      object[] args = new object[4];
      args[0] = builder.GetTimingCategory();
      args[1] = valueObj;
      args[2] = builder.GetTimingName();
      args[3] = builder.GetTimingLabel();
      object map;
      Dictionary<AndroidJavaObject, string> parameters =
          AddCustomVariablesAndCampaignParameters(builder);
      if (parameters != null) {
        map = BuildMap(GoogleAnalyticsV3.TIMING_HIT, args, parameters);
      } else {
        map = BuildMap(GoogleAnalyticsV3.TIMING_HIT, args);
      }
      tracker.Call(GoogleAnalyticsV3.SEND, map);
    }
  }

  public void ClearUserIDOverride() {
    SetTrackerVal(Fields.USER_ID, null);
  }

  public void SetTrackingCode(string trackingCode) {
    this.trackingCode = trackingCode;
  }

  public void SetAppName(string appName) {
    this.appName = appName;
  }

  public void SetBundleIdentifier(string bundleIdentifier) {
    this.bundleIdentifier = bundleIdentifier;
  }

  public void SetAppVersion(string appVersion) {
    this.appVersion = appVersion;
  }

  public void SetDispatchPeriod(int dispatchPeriod) {
    this.dispatchPeriod = dispatchPeriod;
  }

  public void SetLogLevelValue(GoogleAnalyticsV3.DebugMode logLevel) {
    this.logLevel = logLevel;
  }

  public void SetAnonymizeIP(bool anonymizeIP) {
    this.anonymizeIP = anonymizeIP;
  }

  public void SetDryRun(bool dryRun) {
    this.dryRun = dryRun;
  }

#endif
  public void Dispose()
  {
#if UNITY_ANDROID && !UNITY_EDITOR
    googleAnalyticsSingleton.Dispose();
    tracker.Dispose();
    analyticsTrackingFields.Dispose();
#endif
  }

}
