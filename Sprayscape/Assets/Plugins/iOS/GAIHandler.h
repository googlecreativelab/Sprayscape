
#import <Foundation/Foundation.h>
#import "GAIDictionaryBuilder.h"

void _set(char * parameterName, char * value);
char* _get(char * parameterName);
void _send(NSDictionary * parameters);
void _setProductName(char * name);
void _setProductVersion(char * version);
void _setOptOut(bool optOut);
void _setDispatchInterval(int time);
void _setTrackUncaughtExceptions(bool trackUncaughtExceptions);
void _setDryRun(bool dryRun);
id _getTrackerWithName(char* name, char* trackingId);
id _getTrackerWithTrackingId(char* trackingId);
void _removeTrackerByName(char* trackingId);
void _dispatch();


@interface GAIHandler : NSObject

+ (void) addAdditionalParametersToBuilder: (GAIDictionaryBuilder*)builder;
@end
