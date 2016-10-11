#import <AVFoundation/AVFoundation.h>

static const char * CAMERA_PERMISSION = "CAMERA";

@interface PermissionHelper : NSObject

+ (void) showAlertAndShareWithTitle: (const char *) title withMessage:(const char *) message withCompletionHandler:(void (^)(BOOL))completion;
+ (void) requestCameraPermissionWithCompletion:(void (^)(BOOL))completion;
+ (NSString*) createNSString:(const char*) string;

@end

@implementation PermissionHelper

+ (void) showAlertAndShareWithTitle: (const char *) title withMessage:(const char *) message withCompletionHandler:(void (^)(BOOL))completion
{
  NSLog(@"PermissionCallbackReceiver showAlertAndShareWithTitle()");
  NSString * alertTitle = [PermissionHelper createNSString:title];
  NSString * alertMessage = [PermissionHelper createNSString:message];
  BOOL denied = _PermissionCallbackReceiver_IsCameraDenied();
  
  UIAlertController *alertController = [UIAlertController
                                        alertControllerWithTitle:alertTitle
                                        message:alertMessage
                                        preferredStyle:UIAlertControllerStyleAlert];
  
  UIAlertAction *cancelAction = [UIAlertAction
                                 actionWithTitle:@"Cancel"
                                 style:UIAlertActionStyleCancel
                                 handler:^(UIAlertAction *action)
                                 {
                                   [PermissionHelper requestCameraPermissionWithCompletion:completion];
                                 }];
  
  UIAlertAction *acceptAction;
  if(denied && &UIApplicationOpenSettingsURLString != NULL) {
    acceptAction = [UIAlertAction
                    actionWithTitle:@"Settings"
                    style:UIAlertActionStyleDefault
                    handler:^(UIAlertAction *action)
                    {
                      NSURL *appSettings = [NSURL URLWithString:UIApplicationOpenSettingsURLString];
                      [[UIApplication sharedApplication] openURL:appSettings];
                    }];
    
    
  }
  else {
    
    acceptAction = [UIAlertAction
                    actionWithTitle:@"OK"
                    style:UIAlertActionStyleDefault
                    handler:^(UIAlertAction *action)
                    {
                      [PermissionHelper requestCameraPermissionWithCompletion:completion];
                    }];
    
  }
  
  [alertController addAction:cancelAction];
  [alertController addAction:acceptAction];
  
  UIViewController *viewController = UnityGetGLViewController();
  [viewController presentViewController:alertController animated:YES completion:nil];
}

+ (void) requestCameraPermissionWithCompletion:(void (^)(BOOL))completion
{
  NSLog(@"PermissionCallbackReceiver requestCameraPermissionWithCompletion()");
  if ([AVCaptureDevice respondsToSelector:@selector(requestAccessForMediaType: completionHandler:)]) {
    [AVCaptureDevice requestAccessForMediaType:AVMediaTypeVideo completionHandler:^(BOOL granted) {
      NSLog(@"PermissionCallbackReceiver requestCameraPermissionWithCompletion() completed");
      // Will get here on both iOS 7 & 8 even though camera permissions weren't required
      // until iOS 8. So for iOS 7 permission will always be granted.
      if (granted) {
        // Permission has been granted. Use dispatch_async for any UI updating
        // code because this block may be executed in a thread.
        dispatch_async(dispatch_get_main_queue(), ^{
          completion(YES);
        });
      } else {
        completion(NO);
      }
    }];
  } else {
    completion(YES);
  }
}
+ (NSString*) createNSString:(const char*) string
{
  if (string)
    return [NSString stringWithUTF8String: string];
  else
    return [NSString stringWithUTF8String: ""];
}

extern UIViewController* UnityGetGLViewController();

extern "C" {
  bool _PermissionCallbackReceiver_HasCameraPermission (){
    AVAuthorizationStatus status = [AVCaptureDevice authorizationStatusForMediaType:AVMediaTypeVideo];
    if(status == AVAuthorizationStatusAuthorized) {
      return YES;
    }
    return NO;
  }
  void _PermissionCallbackReceiver_RequestCamera (const char * title, const char * message, const char * cObjectName, const char * cMethodName){
    NSString *objectName = [PermissionHelper createNSString:cObjectName];
    NSString *methodName = [PermissionHelper createNSString:cMethodName];
    [PermissionHelper showAlertAndShareWithTitle:title withMessage:message withCompletionHandler:^(BOOL granted) {
      NSString *result = [NSString stringWithFormat:@"%s,%s", CAMERA_PERMISSION, granted ? "true" : "false"];
      NSLog(@"Callback: %@,%@,%@", objectName, methodName, result);
      UnitySendMessage([objectName UTF8String],[methodName UTF8String],[result UTF8String]);
    }];
  }
  bool _PermissionCallbackReceiver_IsCameraDenied (){
    AVAuthorizationStatus status = [AVCaptureDevice authorizationStatusForMediaType:AVMediaTypeVideo];
    if(status == AVAuthorizationStatusDenied) {
      return YES;
    }
    return NO;
  }
  bool _PermissionCallbackReceiver_IsCameraNotAuthorized (){
    AVAuthorizationStatus status = [AVCaptureDevice authorizationStatusForMediaType:AVMediaTypeVideo];
    if(status == AVAuthorizationStatusRestricted) {
      return YES;
    }
    return NO;
  }
}
@end