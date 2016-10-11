/*!
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

#import "GAIHandler.h"
#import "GAI.h"
#import "GAITracker.h"
#import "GAILogger.h"
#import "GAIDictionaryBuilder.h"
#import "GAIFields.h"

@implementation GAIHandler

bool hasCampaignParameters = false;
bool startSessionOnNextHit = false;
bool endSessionOnNextHit = false;
NSDictionary *campaignData;
NSMutableDictionary *customMetrics = nil;
NSMutableDictionary *customDimensions= nil;

GAI* shared_instance() {
    return [GAI sharedInstance];
}

id<GAITracker> gai_default_tracker() {
    return [[GAI sharedInstance] defaultTracker];
}

BOOL getOptOut() {
    return [GAI sharedInstance].optOut;
}

void setOptOut(bool opt_out) {
    [GAI sharedInstance].optOut = opt_out;
}

void anonymizeIP(){
    id tracker = [[GAI sharedInstance] defaultTracker];
    [tracker set:kGAIAnonymizeIp value:@"1"];
}

void setSampleFrequency(int sampleFrequency) {
    id tracker = [[GAI sharedInstance] defaultTracker];
    [tracker set:kGAISampleRate value:[@(sampleFrequency) stringValue]];
}

void setLogLevel(int logLevel) {
    [[GAI sharedInstance].logger setLogLevel:logLevel];
}

void setDispatchInterval(int dispatch_interval) {
    [GAI sharedInstance].dispatchInterval = dispatch_interval;
}

void setTrackUncaughtExceptions(bool track_uncaught_exceptions) {
    [GAI sharedInstance].trackUncaughtExceptions = track_uncaught_exceptions;
}

void setDryRun(bool dry_run) {
    [GAI sharedInstance].dryRun = dry_run;
}

void startSession() {
    startSessionOnNextHit = true;
}

void stopSession() {
    endSessionOnNextHit = true;
}

id<GAITracker> trackerWithName(char *name, char *trackingId) {
    return [[GAI sharedInstance] trackerWithName: [NSString stringWithUTF8String:name]
                                      trackingId: [NSString stringWithUTF8String:trackingId]];
}

id<GAITracker> trackerWithTrackingId(char *trackingId) {
    return [[GAI sharedInstance] trackerWithTrackingId:[NSString stringWithUTF8String:trackingId ]];
}


void dispatch() {
    [[GAI sharedInstance] dispatch];
}

void setBool(const char * parameterName, const BOOL isValue) {
    id<GAITracker> tracker = [[GAI sharedInstance] defaultTracker];
    
    [tracker set:[NSString stringWithUTF8String:parameterName]
           value:isValue ? [@YES stringValue] : [@NO stringValue]];
}

void set(const char * parameterName, const char * value ) {
    id<GAITracker> tracker = [[GAI sharedInstance] defaultTracker];
    
    [tracker set:[NSString stringWithUTF8String:parameterName]
           value:[NSString stringWithUTF8String:value ]];
}

NSString* get(const char * parameterName) {
    id tracker = [[GAI sharedInstance] defaultTracker];
    return [tracker get:[NSString stringWithUTF8String:parameterName]];
}

void addCustomDimensionToDictionary(int index, char * value){
    if (!customDimensions) {
        customDimensions = [[NSMutableDictionary alloc] initWithCapacity:20];
    }
    customDimensions[@(index)] = @(value);
}

void addCustomMetricToDictionary(int index, char * value){
    if (!customMetrics) {
        customMetrics = [[NSMutableDictionary alloc] initWithCapacity:20];
    }
    customMetrics[@(index)] = @(value);
}

+ (void) setCustomDimensionsOnBuilder: (GAIDictionaryBuilder*)builder {
    for(id key in customDimensions) {
        NSString * string = [customDimensions objectForKey:key];
        [builder set:string
              forKey:[GAIFields customDimensionForIndex:[key intValue]]];
    }
    customDimensions = nil;
}

+ (void) setCustomMetricsOnBuilder: (GAIDictionaryBuilder*)builder {
    for(id key in customMetrics) {
        NSString * string = [customMetrics objectForKey:key];
        [builder set:string
              forKey:[GAIFields customMetricForIndex:[key intValue]]];
    }
    customMetrics = nil;
}

void buildCampaignParametersDictionary(const char * source, const char * medium, const char * name, const char * content, const char * keyword) {
    campaignData = [NSDictionary dictionaryWithObjectsAndKeys:
                    [NSString stringWithUTF8String:source], kGAICampaignSource,
                    [NSString stringWithUTF8String:medium], kGAICampaignMedium,
                    [NSString stringWithUTF8String:name], kGAICampaignName,
                    [NSString stringWithUTF8String:content], kGAICampaignContent,
                    [NSString stringWithUTF8String:keyword], kGAICampaignKeyword, nil];
    hasCampaignParameters = true;
}

+ (void) addAdditionalParametersToBuilder: (GAIDictionaryBuilder*)builder {
    if (hasCampaignParameters) {
        [builder setAll: campaignData];
    }
    
    if (startSessionOnNextHit) {
        startSessionOnNextHit = false;
        [builder set:@"start" forKey:kGAISessionControl];
    } else if (endSessionOnNextHit) {
        endSessionOnNextHit = false;
        [builder set:@"end" forKey:kGAISessionControl];
    }
    
}

void sendAppView(const char * value) {
    id tracker = [[GAI sharedInstance] defaultTracker];
    
    GAIDictionaryBuilder *builder = [GAIDictionaryBuilder createAppView];
    [tracker set:kGAIScreenName
           value:[NSString stringWithUTF8String:value]];
    
    [GAIHandler addAdditionalParametersToBuilder:builder];
    [GAIHandler setCustomDimensionsOnBuilder:builder];
    [GAIHandler setCustomMetricsOnBuilder:builder];
    
    [tracker send:[builder build]];
}

void sendEvent(const char * category, const char * action, const char * label, const long long value) {
    id tracker = [[GAI sharedInstance] defaultTracker];
    
    GAIDictionaryBuilder *builder = [GAIDictionaryBuilder createEventWithCategory:[NSString stringWithUTF8String:category]
                                                                           action:[NSString stringWithUTF8String:action]
                                                                            label:[NSString stringWithUTF8String:label]
                                                                            value:[NSNumber numberWithLongLong:value]];
    [GAIHandler addAdditionalParametersToBuilder:builder];
    [GAIHandler setCustomDimensionsOnBuilder:builder];
    [GAIHandler setCustomMetricsOnBuilder:builder];
    
    [tracker send:[builder build]];
}

void sendTransaction( const char * transactionID, const char* affiliation, const double revenue, const double tax, const double shipping, const char* currencyCode) {
    id tracker = [[GAI sharedInstance] defaultTracker];
    
    GAIDictionaryBuilder *builder = [GAIDictionaryBuilder createTransactionWithId:[NSString stringWithUTF8String:transactionID]
                                                                      affiliation:[NSString stringWithUTF8String:affiliation]
                                                                          revenue:[NSNumber numberWithDouble:revenue]
                                                                              tax:[NSNumber numberWithDouble:tax]
                                                                         shipping:[NSNumber numberWithDouble:shipping]
                                                                     currencyCode:[NSString stringWithUTF8String:currencyCode]];
    [GAIHandler addAdditionalParametersToBuilder:builder];
    [GAIHandler setCustomDimensionsOnBuilder:builder];
    [GAIHandler setCustomMetricsOnBuilder:builder];
    
    [tracker send:[builder build]];
}

void sendItemWithTransaction(const char * transactionID, const char * name, const char * sku, const char * category, const double price, const long long quantity, const char * currencyCode) {
    id tracker = [[GAI sharedInstance] defaultTracker];
    
    GAIDictionaryBuilder *builder = [GAIDictionaryBuilder createItemWithTransactionId:[NSString stringWithUTF8String:transactionID]
                                                                                 name:[NSString stringWithUTF8String:name]
                                                                                  sku:[NSString stringWithUTF8String:sku]
                                                                             category:[NSString stringWithUTF8String:category]
                                                                                price:[NSNumber numberWithDouble:price]
                                                                             quantity:[NSNumber numberWithLongLong:quantity]
                                                                         currencyCode:[NSString stringWithUTF8String:currencyCode]];
    
    [GAIHandler addAdditionalParametersToBuilder:builder];
    [GAIHandler setCustomDimensionsOnBuilder:builder];
    [GAIHandler setCustomMetricsOnBuilder:builder];
    
    [tracker send:[builder build]];
}


void sendException(const char * errorDescription, const bool isFatal) {
    id tracker = [[GAI sharedInstance] defaultTracker];
    
    GAIDictionaryBuilder *builder = [GAIDictionaryBuilder createExceptionWithDescription:[NSString stringWithUTF8String:errorDescription]
                                                                               withFatal:isFatal ? @YES : @NO];
    
    [GAIHandler addAdditionalParametersToBuilder:builder];
    [GAIHandler setCustomDimensionsOnBuilder:builder];
    [GAIHandler setCustomMetricsOnBuilder:builder];
    
    [tracker send:[builder build]];
    
}

void sendSocial(const char * socialNetwork, const char * socialAction, const char * targetUrl) {
    id tracker = [[GAI sharedInstance] defaultTracker];
    
    GAIDictionaryBuilder *builder = [GAIDictionaryBuilder createSocialWithNetwork:[NSString stringWithUTF8String:socialNetwork]
                                                                           action:[NSString stringWithUTF8String:socialAction]
                                                                           target:[NSString stringWithUTF8String:targetUrl]];
    
    [GAIHandler addAdditionalParametersToBuilder:builder];
    [GAIHandler setCustomDimensionsOnBuilder:builder];
    [GAIHandler setCustomMetricsOnBuilder:builder];
    
    [tracker send:[builder build]];
}

void sendTiming(const char * timingCategory, const long long timingInterval, const char * name, const char * label) {
    id tracker = [[GAI sharedInstance] defaultTracker];
    
    GAIDictionaryBuilder *builder = [GAIDictionaryBuilder createTimingWithCategory:[NSString stringWithUTF8String:timingCategory]
                                                                          interval:[NSNumber numberWithLongLong:timingInterval]
                                                                              name:[NSString stringWithUTF8String:name]
                                                                             label:[NSString stringWithUTF8String:label]];
    [GAIHandler addAdditionalParametersToBuilder:builder];
    [GAIHandler setCustomDimensionsOnBuilder:builder];
    [GAIHandler setCustomMetricsOnBuilder:builder];
    
    [tracker send:[builder build]];
}

@end
