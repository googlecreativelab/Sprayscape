// Method for image sharing

@interface ShareHelper : NSObject

+(void) sharePath: (const char *) path;
+(void) shareUrl: (const char *) url withMessage:(const char *) message;

@end

@implementation ShareHelper

+(void) sharePath: (const char *) path withCompletionHandler:(void (^)(BOOL success))handler
{
  NSString *imagePath = [NSString stringWithUTF8String:path];
  NSData *data = [NSData dataWithContentsOfFile:imagePath];
  NSArray * excludeActivities = @[UIActivityTypeAssignToContact, UIActivityTypePostToWeibo, UIActivityTypePrint];
  
  UIActivityViewController *activity = [[UIActivityViewController alloc] initWithActivityItems:@[data] applicationActivities:nil];
  activity.excludedActivityTypes = excludeActivities;
  [activity setCompletionWithItemsHandler:^(NSString *activityType, BOOL completed, NSArray *returnedItems, NSError *activityError) {
    handler(completed);
  }];
  
  [[self class] showActivityViewController:activity];
}

+(void) shareUrl: (const char *) url withMessage:(const char *) message withCompletionHandler:(void (^)(BOOL success))handler
{
  NSString *urlString = [NSString stringWithUTF8String:url];
  NSString *messageString = [NSString stringWithUTF8String:message];
  NSArray * activityItems = @[messageString, [NSURL URLWithString:urlString]];
  NSArray * excludeActivities = @[UIActivityTypeAssignToContact, UIActivityTypePostToWeibo, UIActivityTypePrint];
  
  
  UIActivityViewController *activity = [[UIActivityViewController alloc] initWithActivityItems:activityItems applicationActivities:Nil];
  activity.excludedActivityTypes = excludeActivities;
  [activity setCompletionWithItemsHandler:^(NSString *activityType, BOOL completed, NSArray *returnedItems, NSError *activityError) {
    handler(completed);
  }];
  
  [[self class] showActivityViewController:activity];
}

+(void) showActivityViewController:(UIActivityViewController *)activity {
  
  UIViewController *rootViewController = UnityGetGLViewController();
  //if iPhone
  if (UI_USER_INTERFACE_IDIOM() == UIUserInterfaceIdiomPhone) {
    [rootViewController presentViewController:activity animated:YES completion:Nil];
  }
  //if iPad
  else {
    // Change Rect to position Popover
    UIPopoverController *popup = [[UIPopoverController alloc] initWithContentViewController:activity];
    [popup presentPopoverFromRect:CGRectMake(rootViewController.view.frame.size.width/2, rootViewController.view.frame.size.height/4, 0, 0)inView:rootViewController.view permittedArrowDirections:UIPopoverArrowDirectionAny animated:YES];
  }
}

extern UIViewController* UnityGetGLViewController();

extern "C" {
  void _NativeShare_ShareImage(const char * path, const char * cCallbackObjectName, const char * cCallbackMethodName){
    NSString *callbackObjectName = [NSString stringWithFormat:@"%s",cCallbackObjectName];
    NSString *callbackMethodName = [NSString stringWithFormat:@"%s",cCallbackMethodName];
    [ShareHelper sharePath:path withCompletionHandler:^(BOOL success) {
      UnitySendMessage([callbackObjectName UTF8String], [callbackMethodName UTF8String], (success) ? "true" : "false");
    }];
  }
  
  void _NativeShare_ShareUrl(const char * url, const char * message, const char * cCallbackObjectName, const char * cCallbackMethodName){
    NSString *callbackObjectName = [NSString stringWithFormat:@"%s",cCallbackObjectName];
    NSString *callbackMethodName = [NSString stringWithFormat:@"%s",cCallbackMethodName];
    [ShareHelper shareUrl:url withMessage:message withCompletionHandler:^(BOOL success) {
      UnitySendMessage([callbackObjectName UTF8String], [callbackMethodName UTF8String], (success) ? "true" : "false");
    }];
  }
}
@end