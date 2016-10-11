#import <TargetConditionals.h>
#import <AvailabilityMacros.h>

#if __has_feature(objc_arc)
#error "This file needs to be compiled with ARC disabled."
#endif

#ifndef GTM_USE_SESSION_FETCHER
  #define GTM_USE_SESSION_FETCHER 1
#endif

#import "gtm-oauth2/Source/GTMOAuth2Authentication.h"
#import "gtm-oauth2/Source/GTMOAuth2SignIn.h"
#import "gtm-oauth2/Source/Touch/GTMOAuth2ViewControllerTouch.h"
