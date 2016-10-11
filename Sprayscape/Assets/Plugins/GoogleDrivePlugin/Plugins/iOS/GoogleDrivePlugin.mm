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

#define GTLR_USE_FRAMEWORK_IMPORTS 0

#import "GTLRDrive.h"
#import "GTMOAuth2ViewControllerTouch.h"
#import "Reachability.h"


static const char * CALLBACK_METHOD_DRIVE_ACCOUNT_SELECTED = "DriveAccountSelected";
static const char * CALLBACK_METHOD_NO_INTERNET_CONNECTION = "DriveNotOnline";
static const char * CALLBACK_METHOD_DRIVE_IS_READY = "DriveIsReady";
static const char * CALLBACK_METHOD_DRIVE_FILE_UPLOADED = "DriveFileUploaded";
static const char * CALLBACK_METHOD_DRIVE_AUTH_FAILED = "DriveAuthFailed";
static const char * CALLBACK_METHOD_DRIVE_PERMISSION_FAILED = "DrivePermissionChangeFailed";
static const char * CALLBACK_METHOD_DRIVE_UPLOAD_FAILED = "DriveUploadFailed";
static const char * CALLBACK_METHOD_DRIVE_FILE_EXISTS = "DriveFileExists";

static NSString *const mimeType             = @"image/jpeg";
static NSString *const kUserEmailKey        = @"userEmail";

@interface GoogleDrivePlugin : NSObject

@property (nonatomic, strong) NSString * driveFolderName;
@property (nonatomic, strong) NSString * driveFileName;
@property (nonatomic, strong) NSString * localPath;
@property (nonatomic, strong) NSString * callbackObjectName;
@property (nonatomic, strong) NSString * clientId;
@property (nonatomic, strong) NSString * keychainName;
@property (nonatomic, strong) NSString * userEmail;

@property (nonatomic, strong) GTLRDriveService * service;


- (id) initWithClientId:(NSString *) pClientId withKeychainName:(NSString *)pKeychainName withCallbackObjectName:(NSString *) pCallbackObjectName;

- (void) checkAccountPermissions;
- (void) uploadWithFolderName:(NSString *)pDriveFolderName withFileName:(NSString *)pDriveFileName withLocalPath:(NSString *)pLocalPath;
- (void) checkIfFileExists:(NSString *)fileId;

@end

@implementation GoogleDrivePlugin

NSString *scopes = @"https://www.googleapis.com/auth/drive";
NSString *folderId;
UIViewController *rootViewController;

@synthesize driveFolderName;
@synthesize driveFileName;
@synthesize localPath;
@synthesize callbackObjectName;
@synthesize clientId;
@synthesize keychainName;
@synthesize userEmail;

@synthesize service;

- (id) initWithClientId:(NSString *) pClientId withKeychainName:(NSString *)pKeychainName withCallbackObjectName:(NSString *) pCallbackObjectName
{
  clientId = pClientId;
  keychainName = pKeychainName;
  callbackObjectName = pCallbackObjectName;

  service = [[GTLRDriveService alloc] init];
  service.authorizer = [GTMOAuth2ViewControllerTouch authForGoogleFromKeychainForName:keychainName clientID:clientId clientSecret:nil];

  rootViewController = UnityGetGLViewController();

  return self;

}

- (BOOL) isConnected {
  if (!service.authorizer.canAuthorize) {
    // Not yet authorized, request authorization by pushing the login UI onto the UI stack.
    return NO;
  }
  return YES;
}

- (void) uploadWithFolderName:(NSString *)pDriveFolderName withFileName:(NSString *)pDriveFileName withLocalPath:(NSString *)pLocalPath
{
  driveFolderName = pDriveFolderName;
  driveFileName = pDriveFileName;
  localPath = pLocalPath;
  [self uploadImage];
}

- (void) checkAccountPermissions {

  Reachability *networkReachability = [Reachability reachabilityForInternetConnection];
  NetworkStatus networkStatus = [networkReachability currentReachabilityStatus];
  if (networkStatus == NotReachable) {
    [self callUnityObject:[callbackObjectName UTF8String] withMethod:CALLBACK_METHOD_NO_INTERNET_CONNECTION withParameter:""];
    return;
  }
  if (!service.authorizer.canAuthorize) {
    // Not yet authorized, request authorization by pushing the login UI onto the UI stack.
    NSLog(@"GoogleDrivePlugin: Presenting view controller!");
    [rootViewController presentViewController:[self createAuthController] animated:YES completion:nil];
  } else {
    NSUserDefaults *userDefaults = [NSUserDefaults standardUserDefaults];
    userEmail = [userDefaults objectForKey:kUserEmailKey];
    NSLog(@"GoogleDrivePlugin: canAuthorize! email: %@", userEmail);

    if(userEmail == nil) {
      [self clearCredentials];
      [self checkAccountPermissions];
      return;
    }

    [self callUnityObject:[callbackObjectName UTF8String] withMethod:CALLBACK_METHOD_DRIVE_ACCOUNT_SELECTED withParameter:[userEmail UTF8String]];
    [self callUnityObject:[callbackObjectName UTF8String] withMethod:CALLBACK_METHOD_DRIVE_IS_READY withParameter:[userEmail UTF8String]];
  }
}


// Handle completion of the authorization process, and update the Drive API
// with the new credentials.
- (void)viewController:(GTMOAuth2ViewControllerTouch *)viewController
      finishedWithAuth:(GTMOAuth2Authentication *)authResult
                 error:(NSError *)error {
  if (error != nil) {
    [rootViewController dismissViewControllerAnimated:YES completion:nil];
    [self clearCredentials];
    [self callUnityObject:[callbackObjectName UTF8String] withMethod:CALLBACK_METHOD_DRIVE_AUTH_FAILED withParameter:[[error description] UTF8String]];

  }
  else {
    userEmail = authResult.userEmail;
    NSLog(@"GoogleDrivePlugin: authorized! email: %@", userEmail);

    NSUserDefaults *userDefaults = [NSUserDefaults standardUserDefaults];
    [userDefaults setObject:userEmail forKey:kUserEmailKey];
    [userDefaults synchronize];

    service.authorizer = authResult;
    [rootViewController dismissViewControllerAnimated:YES completion:nil];

    [self callUnityObject:[callbackObjectName UTF8String] withMethod:CALLBACK_METHOD_DRIVE_ACCOUNT_SELECTED withParameter:[userEmail UTF8String]];
    [self callUnityObject:[callbackObjectName UTF8String] withMethod:CALLBACK_METHOD_DRIVE_IS_READY withParameter:[userEmail UTF8String]];
  }
}

// Creates the auth controller for authorizing access to Drive API.
- (GTMOAuth2ViewControllerTouch *)createAuthController {
  GTMOAuth2ViewControllerTouch *authController;
  // If modifying these scopes, delete your previously saved credentials by
  // resetting the iOS simulator or uninstall the app.
  authController = [[GTMOAuth2ViewControllerTouch alloc]
                    initWithScope:scopes
                    clientID:clientId
                    clientSecret:nil
                    keychainItemName:keychainName
                    delegate:self
                    finishedSelector:@selector(viewController:finishedWithAuth:error:)];
  return authController;
}

- (void)callUnityObject:(const char*)object withMethod:(const char*)method withParameter:(const char*)parameter {
  UnitySendMessage(object, method, parameter);
}

- (void)sendFailureToUnity: (NSString *) reason {
  if(reason == nil) {
    reason = @"";
  }
  [self callUnityObject:[callbackObjectName UTF8String] withMethod:CALLBACK_METHOD_DRIVE_UPLOAD_FAILED withParameter:[reason UTF8String]];
}

- (void) clearCredentials {
  [GTMOAuth2ViewControllerTouch removeAuthFromKeychainForName:keychainName];
  service.authorizer = nil;
  NSUserDefaults *userDefaults = [NSUserDefaults standardUserDefaults];
  [userDefaults removeObjectForKey:kUserEmailKey];
  [userDefaults synchronize];
}

// Helper for showing an alert
- (void)showAlert:(NSString *)title message:(NSString *)message {
  UIAlertController *alert = [UIAlertController alertControllerWithTitle:title message:message preferredStyle:UIAlertControllerStyleAlert];
  UIAlertAction *ok =
  [UIAlertAction actionWithTitle:@"OK"
                           style:UIAlertActionStyleDefault
                         handler:^(UIAlertAction * action)
   {
     [alert dismissViewControllerAnimated:YES completion:nil];
   }];
  [alert addAction:ok];
  [rootViewController presentViewController:alert animated:YES completion:nil];

}

- (void) uploadImage {
  __block NSData *data = [NSData dataWithContentsOfFile:localPath];
  [self createFolderIfNotExist:driveFolderName withCompletionHandler:^(GTLRDrive_File * folder, NSError *error) {
    if(error != nil) {
      NSString *reason = [[error.userInfo objectForKey:kGTMOAuth2ErrorJSONKey] objectForKey:@"error"];
      if([reason isEqualToString:@"invalid_grant"]) {
        NSLog(@"Invalid grant of permissions!");
        [self clearCredentials];
      }
      [self sendFailureToUnity:error.description];
    }
    else {
      [self uploadImageFile:data withFileName:driveFileName withParentFolderId:folder.identifier withCompletionHandler:^(GTLRDrive_File *file, NSError *error) {
        if(error != nil) {
          NSLog(@"%@", error);
          [self sendFailureToUnity:error.description];
        }
        else {
          [self callUnityObject:[callbackObjectName UTF8String] withMethod:CALLBACK_METHOD_DRIVE_FILE_UPLOADED withParameter:[file.identifier UTF8String]];
        }
      }];
    }
  }];
}

- (void) checkIfFileExists:(NSString *)fileId
{
  GTLRDriveQuery_FilesGet *query = [GTLRDriveQuery_FilesGet queryWithFileId:fileId];
  [service executeQuery:query completionHandler:^(GTLRServiceTicket * _Nonnull callbackTicket, id  _Nullable object, NSError * _Nullable callbackError) {
    if(callbackError != nil) {
      [self callUnityObject:[callbackObjectName UTF8String] withMethod:CALLBACK_METHOD_DRIVE_FILE_EXISTS withParameter:"False"];
    }
    else {
      [self callUnityObject:[callbackObjectName UTF8String] withMethod:CALLBACK_METHOD_DRIVE_FILE_EXISTS withParameter:"True"];
    }
  }];
}

- (void) createFolderIfNotExist:(NSString *) pDriveFolderName withCompletionHandler:(void (^)(GTLRDrive_File * file, NSError *error))handler {
  [self ensureDriveFolderExists:pDriveFolderName withCompletionHandler:^(GTLRDrive_File * folder, NSError *error) {
    if(error != nil) {
      handler(nil, error);
    }
    else {
      if(folder == nil) {
        [self createFolderWithName:pDriveFolderName withCompletionHandler:^(GTLRDrive_File *file, NSError *error) {
          handler(file, nil);
        }];
      }
      else {
        handler(folder, nil);
      }
    }
  }];
}

- (void) uploadImageFile: (NSData *) data withFileName: (NSString *) pFileName withParentFolderId:(NSString *) pFolderId withCompletionHandler:(void (^)(GTLRDrive_File * file, NSError *error))handler {
  NSLog(@"GoogleDrivePlugin: Uploading image with file name: %@", pFileName);

  GTLRDrive_File *metadata = [GTLRDrive_File object];
  metadata.name = pFileName;
  metadata.parents = @[pFolderId];

  //NSData *data = [NSData dataWithContentsOfFile:localPath];

  GTLRUploadParameters *uploadParameters = [GTLRUploadParameters uploadParametersWithData:data MIMEType:mimeType];
  GTLRDriveQuery_FilesCreate *query = [GTLRDriveQuery_FilesCreate queryWithObject:metadata uploadParameters:uploadParameters];
  [service executeQuery:query completionHandler:^(GTLRServiceTicket *ticket, GTLRDrive_File *updatedFile, NSError *error) {

    if (error == nil) {
      NSLog(@"GoogleDrivePlugin: Uploading image complete: %@", pFileName);
      __block NSString *fileID = updatedFile.identifier;
      __block GTLRDrive_File *finalFile = updatedFile;
      GTLRDrive_Permission *newPermission = [GTLRDrive_Permission object];
      newPermission.type = @"anyone";
      newPermission.role = @"reader";
      GTLRDriveQuery_PermissionsCreate *permissionQuery = [GTLRDriveQuery_PermissionsCreate queryWithObject:newPermission fileId:fileID];
      NSLog(@"GoogleDrivePlugin: Setting image permissions: %@", pFileName);
      [service executeQuery:permissionQuery completionHandler:^(GTLRServiceTicket *ticket, GTLRDrive_Permission *permission, NSError *error) {

        if (error == nil) {
          NSLog(@"GoogleDrivePlugin: Permissions set: %@", pFileName);

          GTLRDriveQuery_FilesGet *returnFileQuery = [GTLRDriveQuery_FilesGet queryWithFileId:fileID];
          returnFileQuery.fields = @"webContentLink, webViewLink";
          [service executeQuery:returnFileQuery completionHandler:^(GTLRServiceTicket *ticket, GTLRDrive_File *file, NSError *error) {
            if (error == nil) {
              NSLog(@"GoogleDrivePlugin: File details for: %@", pFileName);
              NSLog(@"->fileIdentifier: %@", finalFile.identifier);
              NSLog(@"->webContentLink: %@", finalFile.webContentLink);
              NSLog(@"->JSONString: %@", finalFile.JSONString);
              NSLog(@"->webViewLink: %@", finalFile.webViewLink);
              handler(finalFile, nil);
            } else {
              NSLog(@"GoogleDrivePlugin: An error occurred retrieving file: %@", error);
              handler(nil, error);
            }
          }];

        } else {
          NSLog(@"GoogleDrivePlugin: An error occurred setting permissions on file: %@", error);
          [self callUnityObject:[callbackObjectName UTF8String] withMethod:CALLBACK_METHOD_DRIVE_PERMISSION_FAILED withParameter:[finalFile.identifier UTF8String]];
          handler(nil, error);
        }
      }];

    } else {
      NSLog(@"GoogleDrivePlugin: An error occurred uploading file: %@", error);
      handler(nil, error);
    }

  }];


}


- (void) ensureDriveFolderExists:(NSString *) pDriveFolderName withCompletionHandler:(void (^)(GTLRDrive_File * file, NSError *error))handler {
  NSLog(@"GoogleDrivePlugin: Creating folder with name %@", pDriveFolderName);
  GTLRDriveQuery_FilesList *queryFilesList = [GTLRDriveQuery_FilesList query];
  queryFilesList.q = [NSString stringWithFormat:@"mimeType='application/vnd.google-apps.folder' and name = '%@' and trashed=false", pDriveFolderName];
  __block BOOL folderExists = NO;

  __block GTLRDrive_File *folderFile;
  [service executeQuery:queryFilesList completionHandler:^(GTLRServiceTicket *ticket, GTLRDrive_FileList *files, NSError *error) {
    if(error == nil){
      NSMutableArray *dFiles = [[NSMutableArray alloc] init];
      [dFiles addObjectsFromArray:files.files];

      long dfileSize = [dFiles count];

      if(dfileSize > 0){

        folderExists = YES;
        for(GTLRDrive_File *file in dFiles){
          NSLog(@"GoogleDrivePlugin: Folder found is %@", file.name);
          folderId = file.identifier;
          [self ensureDriveFolderPermissions: file withCompletionHandler:handler];
          return;
        }

      }

      handler(nil, nil);
    }
    else {
      NSLog(@"GoogleDrivePlugin: Error finding folder because: %@", error);
      handler(nil, error);
    }
  }];
}

- (void) ensureDriveFolderPermissions:(GTLRDrive_File *) folder withCompletionHandler:(void (^)(GTLRDrive_File * file, NSError *error))handler {
  __block GTLRDrive_File *updatedFolder = folder;

  GTLRDrive_Permission *newPermission = [GTLRDrive_Permission object];
  newPermission.type = @"anyone";
  newPermission.role = @"reader";
  GTLRDriveQuery_PermissionsCreate *permissionQuery = [GTLRDriveQuery_PermissionsCreate queryWithObject:newPermission fileId:updatedFolder.identifier];
  [service executeQuery:permissionQuery completionHandler:^(GTLRServiceTicket *ticket, GTLRDrive_Permission *permission, NSError *error) {
    if (error != nil) {
      NSLog(@"GoogleDrivePlugin: An error creating folder permissions occured: %@", error);
      [self callUnityObject:[callbackObjectName UTF8String] withMethod:CALLBACK_METHOD_DRIVE_PERMISSION_FAILED withParameter:[updatedFolder.identifier UTF8String]];
      [self clearCredentials];
      handler(nil, error);
    }
    else {
      handler(updatedFolder, nil);
    }

  }];
}

- (void) createFolderWithName:(NSString *) pDriveFolderName withCompletionHandler:(void (^)(GTLRDrive_File * file, NSError *error))handler {

  GTLRDrive_File *folder = [GTLRDrive_File object];

  folder.name = pDriveFolderName;
  folder.mimeType = @"application/vnd.google-apps.folder";
  NSLog(@"GoogleDrivePlugin: Creating folder with name %@", pDriveFolderName);


  GTLRDriveQuery_FilesCreate *query = [GTLRDriveQuery_FilesCreate queryWithObject:folder uploadParameters:nil];
  [service executeQuery:query completionHandler: ^(GTLRServiceTicket *ticket, GTLRDrive_File *updatedFile, NSError *error) {
    if (error == nil) {
      NSLog(@"GoogleDrivePlugin: Created folder");
      [self ensureDriveFolderPermissions:updatedFile withCompletionHandler:handler];
    }
    else {
      NSLog(@"GoogleDrivePlugin: An error creating folder occured: %@", error);
      handler(nil, error);
    }

  }];

}

NSString* CreateNSString (const char* string)
{
  if (string)
    return [NSString stringWithUTF8String: string];
  else
    return [NSString stringWithUTF8String: ""];
}


// globally declare image sharing method
extern "C" {
  void _GoogleDrivePlugin_UploadFile(const char * cDriveFolderName, const char * cDriveFileName, const char * cLocalPath, const char * cCallbackObjectName, const char * cClientId, const char * cKeychainName){
    NSLog(@"_GoogleDrivePlugin_UploadFile");

    //convert strings
    NSString *driveFolderName = CreateNSString(cDriveFolderName);
    NSString *driveFileName = CreateNSString(cDriveFileName);
    NSString *localPath = CreateNSString(cLocalPath);
    NSString *callbackObjectName = CreateNSString(cCallbackObjectName);
    NSString *clientId = CreateNSString(cClientId);
    NSString *keychainName = CreateNSString(cKeychainName);

    GoogleDrivePlugin *plugin = [[GoogleDrivePlugin alloc] initWithClientId:clientId withKeychainName:keychainName withCallbackObjectName:callbackObjectName];
    [plugin uploadWithFolderName:driveFolderName withFileName:driveFileName withLocalPath:localPath];
  }
  void _GoogleDrivePlugin_CheckFileId(const char * cFileId, const char * cCallbackObjectName, const char * cClientId, const char * cKeychainName) {
    NSString *fileId = CreateNSString(cFileId);
    NSString *clientId = CreateNSString(cClientId);
    NSString *keychainName = CreateNSString(cKeychainName);
    NSString *callbackObjectName = CreateNSString(cCallbackObjectName);

    GoogleDrivePlugin *plugin = [[GoogleDrivePlugin alloc] initWithClientId:clientId withKeychainName:keychainName withCallbackObjectName:callbackObjectName];
    [plugin checkIfFileExists:fileId];
  }
  void _GoogleDrivePlugin_CheckPermissions(const char * cCallbackObjectName, const char * cClientId, const char * cKeychainName) {
    NSString *clientId = CreateNSString(cClientId);
    NSString *keychainName = CreateNSString(cKeychainName);
    NSString *callbackObjectName = CreateNSString(cCallbackObjectName);

    GoogleDrivePlugin *plugin = [[GoogleDrivePlugin alloc] initWithClientId:clientId withKeychainName:keychainName withCallbackObjectName:callbackObjectName];
    [plugin checkAccountPermissions];
  }
}

@end
