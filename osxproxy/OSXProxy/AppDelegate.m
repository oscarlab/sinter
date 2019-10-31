/* Copyright (C) 2014--2018 Stony Brook University
   Copyright (C) 2016--2018 The University of North Carolina at Chapel Hill

   This file is part of the Sinter Remote Desktop System.

   Sinter is dual-licensed, available under a commercial license or
   for free subject to the LGPL.  

   Sinter is free software: you can redistribute it and/or modify it
   under the terms of the GNU Lesser General Public License as published by
   the Free Software Foundation, either version 3 of the License, or
   (at your option) any later version.  Sinter is distributed in the
   hope that it will be useful, but WITHOUT ANY WARRANTY; without even
   the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
   PURPOSE.  See the GNU Lesser General Public License for more details.  You
   should have received a copy of the GNU Lesser General Public License along
   with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

/*
    AppDelegate.m
    NVRDP

    Created by Syed Masum Billah on 7/31/14.
*/

#import "AppDelegate.h"
#import "CustomWindowController.h"
#import "Model.h"
#import "ControlTypes.h"
#import "XMLTags.h"
#import "Config.h"

#import "ClientHandler.h"


@implementation AppDelegate


@synthesize window;
@synthesize pid;
@synthesize remoteProcesses;
@synthesize processModel;
@synthesize processTable;
@synthesize remoteWindowControllers;
@synthesize ConnectButton;
@synthesize LoadButton;
@synthesize DisconnectButton;
@synthesize ServerIPTextField;
@synthesize ServerPortTextField;
@synthesize PasscodeTextField;


//CFTimeInterval startTime, elapsedTime;

+ (void) initialize {
    serviceCodes = [Config getServiceCodes];
}

+ (NSDictionary *) readConfigFile:(NSString *) plistName{
    NSError *errorDesc;
    NSPropertyListFormat format;
    NSString *plistPath;
    
    plistPath = [[NSBundle mainBundle] pathForResource:plistName ofType:@"plist"];
    NSData *plistXML = [[NSFileManager defaultManager] contentsAtPath:plistPath];
    NSDictionary *config = (NSDictionary *)[NSPropertyListSerialization
                                            propertyListWithData:plistXML
                                            options:NSPropertyListImmutable
                                            format:&format error: &errorDesc];
    if (!config) {
        NSLog(@"Error reading plist: %@, format: %lu", errorDesc, (unsigned long)format);
        return nil;
    }
    return config;
}

- (BOOL)applicationShouldTerminateAfterLastWindowClosed:(NSApplication *) sender{
    return YES;
}

- (void)applicationDidFinishLaunching:(NSNotification *)aNotification{
    // read the settings file
    [window makeFirstResponder:ConnectButton];
    NSDictionary *settings =  [AppDelegate readConfigFile:@"Settings"];
    if (!settings) {
        NSLog(@"Settings.plist file not found, exiting now");
        exit(EXIT_FAILURE);
    }
    
    [ServerIPTextField setStringValue:[settings objectForKey:@"server_ip"]];
    [ServerPortTextField setStringValue:[[settings objectForKey:@"port"] stringValue]];
    [PasscodeTextField setStringValue:@"123456"];

    [[NSNotificationCenter defaultCenter]
        addObserver:self
        selector:@selector(receivedMessage:)
        name:@"AppDelegate"
        object:nil];
    
    [[NSNotificationCenter defaultCenter]
     addObserver:self
     selector:@selector(connectionStatusChanged:)
     name:@"connectedInd" //Indication of connection established
     object:nil];
    
    [[NSNotificationCenter defaultCenter]
     addObserver:self
     selector:@selector(connectionStatusChanged:)
     name:@"disconnectedInd" //Indication of disconnection
     object:nil];

    //shared connection
    sharedConnection = [ClientHandler sharedConnectionWith:[ServerIPTextField stringValue]
                                                   andPort:[[ServerPortTextField stringValue] intValue]];
    processModel = nil;
    isLoaded = NO;
    remoteWindowControllers = [[NSMutableDictionary alloc] init];
    
    //table-view
    [processTable setDataSource:self];
    [processTable setDelegate:self];
    [processTable setTarget:self ];
    
    //[window makeFirstResponder:processTable];
    //log message
    NSLog(@"proxy application loaded successfully");
}

#pragma clang diagnostic ignored "-Wdeprecated-declarations"
- (IBAction) connect:(id)sender{
    [window makeFirstResponder:nil];
    
    if (sharedConnection.isConnected) {
        //NSRunAlertPanel(@"Successfully connected to server.", @"Please press the Load Processes button", @"Ok", nil, nil);
        return;
    }
    
    if(!sharedConnection.isConnected){
        [sharedConnection setIPAndPort:[ServerIPTextField stringValue]
                           andPort:[[ServerPortTextField stringValue] intValue]];
        [sharedConnection initForClientSocket];
        //usleep(1000000);
        //NSRunAlertPanel(@"Connection request is sent", @"Please press the Load Processes button next", @"Ok", nil, nil);
    }
}

- (void) disconnect {
    if (sharedConnection.isConnected) {
        [sharedConnection close];
    }
    [[NSNotificationCenter defaultCenter] postNotificationName:@"disconnectedInd" object:self];
}

- (IBAction) disconnectButtonClicked:(id)sender {
    [self disconnect];
    NSLog(@"Disconnect by user");
}

- (void) removeWindowWithPID:(NSString*) _pid andUniqueId:(NSString*) _uniqueId{
    //< pid, [wind_id1, win_id2, win_id3...]>
    NSMutableArray* windows = [remoteWindowControllers objectForKey:_pid];
    if (!windows){
        return;
    }
    
    for (int i = 0 ; i < [windows count] ; i++ ) {
        CustomWindowController * rwc = windows[i];
        if ([rwc.rmUiRoot.unique_id isEqualToString:_uniqueId]) {
            [windows removeObject:rwc];
            [rwc setShouldClose:YES];
            [[rwc window] performClose:self];
            rwc = nil;
            //NSLog(@"sub-window %@ closed", [[rwc rmUiRoot] name]);
            break;
        }
    }
    if (![windows count]) {
        [remoteWindowControllers removeObjectForKey:windows];
        for (int i=0; i< [remoteProcesses count]; i++) {
            if ([[remoteProcesses[i] process_id] isEqualToString:_pid]) {
                [remoteProcesses removeObjectAtIndex:i];
                i--;
                [processTable reloadData];
                break;
            }
        }
    }
}

- (void) removeAllWindowsWithPID: (NSString*) _pid {
    NSMutableArray* windows = [remoteWindowControllers objectForKey:_pid];
    if (!windows)
        return;
    
    while([windows count] > 0) {
        [self removeWindowWithPID:_pid
            andUniqueId:[[windows[0] rmUiRoot] unique_id]];
    }
}

- (void) removeAllWindows {
    NSArray* keys = [remoteWindowControllers allKeys];
    for (int i = 0 ; i < [keys count] ; i++ ) {
        [self removeAllWindowsWithPID:keys[i]];
    }
}


- (void) addWindow:(CustomWindowController *) rwc havingPID:(NSString*) _pid {
    //< pid, [wind_id1, win_id2, win_id3...]>
    NSMutableArray* windows;
    if (![remoteWindowControllers objectForKey:_pid]){
        windows = [[NSMutableArray alloc] init];
        [windows addObject:rwc];
        [remoteWindowControllers setObject:windows forKey:_pid];
        return;
    }
    
    BOOL isExist = NO;
    windows = [remoteWindowControllers objectForKey:_pid];
    for (int i = 0 ; i < [windows count] ; i++ ) {
        CustomWindowController * _rwc = windows[i];
        if ([rwc.rmUiRoot.unique_id isEqualToString:_rwc.rmUiRoot.unique_id]) {
            _rwc = rwc;
            isExist = YES;
            NSLog(@"subwindow %@ exists, updating it", [[rwc rmUiRoot] name]);
            break;
        }
    }
    if (!isExist){
        [windows addObject:rwc];
    }
}

- (CustomWindowController *) getWindowWithPID:(NSString *) _pid andUniqueId:(NSString*) uniqueId {
    //< pid, [wind_id1, win_id2, win_id3...]>
    NSMutableArray* windows = [remoteWindowControllers objectForKey:_pid];
    if (!windows) {
        return nil;
    }
    
    for (int i = 0 ; i < [windows count] ; i++ ) {
        CustomWindowController * _rwc = windows[i];
        if ([_rwc.rmUiRoot.unique_id isEqualToString:uniqueId]) {
            return _rwc;
        }
    }
    return nil;
}

- (void) connectionStatusChanged:(NSNotification *) notification {
    //NSLog(@"Notification name = %@", [notification name]);
    if ([[notification name] caseInsensitiveCompare:@"connectedInd"] == NSOrderedSame){
        ConnectButton.enabled = NO;
        ServerPortTextField.enabled = NO;
        ServerIPTextField.enabled = NO;
        PasscodeTextField.enabled = NO;
        LoadButton.enabled = YES;
        DisconnectButton.enabled = YES;
        [sharedConnection sendPasscodeVerifyReq:[PasscodeTextField stringValue]];
        [window makeFirstResponder:LoadButton];

    }
    else if ([[notification name] caseInsensitiveCompare:@"disconnectedInd"] == NSOrderedSame){
        ConnectButton.enabled = YES;
        ServerPortTextField.enabled = YES;
        ServerIPTextField.enabled = YES;
        PasscodeTextField.enabled = YES;
        LoadButton.enabled = NO;
        DisconnectButton.enabled = NO;
        [self removeAllWindows];
        [remoteProcesses removeAllObjects];
        [processTable reloadData];
        [window makeFirstResponder:ConnectButton];
  
    }
}

- (void) receivedMessage:(NSNotification *) notification {
    NSLog(@"Notification name = %@", [notification name]);
    NSDictionary *userInfo = notification.userInfo;
    if(userInfo) {
        Sinter *sinter = [userInfo objectForKey:@"sinter"];
        [self takeActionForXML:sinter];
    }
}

// MARK: ls (list all remote processes)
- (void) takeActionForXML:(Sinter *) sinter {
    NSNumber * service_code = sinter.header.service_code;
    
    if ([service_code isEqualToNumber: [serviceCodes objectForKey:STRLsRes]]) {
        NSArray* apps = sinter.entities;
        if (!apps)  return;
        
        remoteProcesses = [[NSMutableArray alloc] initWithCapacity:[apps count]];
        for (Entity * entity in apps){
            Model * remote_process          = [[Model alloc] initWithEntity:entity];
            [remoteProcesses addObject:remote_process];
        }
        [processTable reloadData];
    }
    
    else if ([service_code isEqualToNumber: [serviceCodes objectForKey:STRLsLongRes]]) {
        NSString * processId = sinter.header.process_id;
        NSString * uniqueId  = sinter.entity.unique_id;
        Entity * entity      = sinter.entity;
        
        CustomWindowController *rmWinController = [self getWindowWithPID:processId andUniqueId:uniqueId];
        if (!rmWinController) {
               rmWinController = [[CustomWindowController alloc]
                                  initWithWindowNibName:@"RemoteWindowController" fromEntity:entity havingProcessID:processId moreEntities:sinter.entities];
               [self addWindow:rmWinController havingPID:processId];
        }
        else if([rmWinController shouldClose]) {
            [rmWinController comeIntoView];
        }
    }
    else if ([service_code isEqualToNumber: [serviceCodes objectForKey:STRVerifyPasscode]]) {
        NSString* result = sinter.header.params.data1;
        NSLog(@"verify_passcode_res result = %@", result);
        
        if([result caseInsensitiveCompare:@"False"] == NSOrderedSame)
        {
            NSAlert *alert = [[NSAlert alloc] init];
            [alert setMessageText:@"Passcode not correct"];
            [alert runModal];
            [self disconnect];
            NSLog(@"Disconnect due to wrong passcode");
        }
    }
    else if ([service_code isEqualToNumber: [serviceCodes objectForKey:STREvent]]){
        NSLog(@"sub_code = %@", sinter.header.sub_code);
        NSString * processId      = sinter.header.process_id;
        if(sinter.header.params.target_id == nil)
            [self removeAllWindowsWithPID:processId];
        else
            [self removeWindowWithPID:processId andUniqueId:sinter.header.params.target_id];
    }
    else {
        NSLog(@"service code %@ is not handled?", service_code);
    }
    
//    switch ([xmlDoc.header.service_code intValue]) {
//
//        case SERVICE_CODE_LS_WINDOW_RES:{
//            
//            NSString * processId = xmlDoc.header.process_id;
//            NSString* uniqueId   = xmlDoc.entity.unique_id;
//
//            CustomWindowController *rmWinController = [self getWindowWithPID:processId andUniqueId:uniqueId];
//            if (!rmWinController) {
//                rmWinController = [[CustomWindowController alloc] initWithWindowNibName:@"RemoteWindowController"
//                                                                        fromXML:xmlDoc havingProcessID:processId];
//                
//                [self addWindow:rmWinController havingPID:processId];
//                
//            }
//            
//        } break;
//            
//        case SERVICE_CODE_CLOSE_WINDOW:{
//            // just header arrived
//            NSDictionary* attrs  = [XMLParser getAllHeaderAttributes:xmlDoc];
//            NSString * _pid      = [attrs objectForKey:processIdTag];
//            NSString * targetId  = [attrs objectForKey:targetIdTag];
//            // remove
//            [self removeWindowWithPID:_pid andUniqueId:targetId];
//        } break;
//            
//        default:
//            break;
//    }
    //NSLog(@"action taken");
}

// ls request
- (IBAction) fetchRemoteProcesses:(id)sender {
    if (!sharedConnection.isConnected) {
        NSRunAlertPanel(@"Connection problem", @"Please press Connect button first", @"Ok", nil, nil);
        return;
    }
    [self removeAllWindows];    
    [sharedConnection sendListRemoteApp];
}

// MARK: delegate method for tableView
- (NSInteger) numberOfRowsInTableView:(NSTableView *)tableView{
    if (remoteProcesses && [remoteProcesses count] ) {
        return [remoteProcesses count];
    }
    return 0;
}
- (NSView *) tableView: (NSTableView *) tableView viewForTableColumn:(NSTableColumn *)tableColumn row:(NSInteger) row {
    NSButton *result = [tableView makeViewWithIdentifier:pid owner:self];
    if (result == nil) {
        result = [[NSButton alloc] init];
        result.identifier = pid;
    }
    
    Model* tag = [remoteProcesses objectAtIndex:row];
    result.title = @"";
    if ([tag name]) {
        result.title = [tag name];
        [result setTarget:self];
        [result setAction:@selector(selectRemoteProcess:)];
        [result setTag:[remoteProcesses indexOfObject:tag]];
    }
    return result;
}

// MARK: ls_l, table-cell double click
- (void) selectRemoteProcess: (id) process_id {
    NSButton *temp = process_id;
    int process_index = (int)[temp tag];
    if (process_index >= 0 && process_index < remoteProcesses.count) {
        processModel = remoteProcesses[process_index];
        if (!processModel || !sharedConnection.isConnected){
            NSLog(@"either process or connection has a problem");
            return;
        }
        
        // construct a ls_request message
        [sharedConnection sendDomRemoteApp:[processModel process_id]];
        
        /*
        Sinter * sinter = [[Sinter alloc] init];
        Header *header = [[Header alloc] init] ;
        header.service_code = [serviceCodes objectForKey:@"ls_l_req"];
        
        NSLog(@"sending ls:%@", [processModel process_id]);
        header.process_id = [processModel process_id];
        sinter.header = header;
        
        //startTime = CACurrentMediaTime();
        //NSLog(@"[sinter] - :%.0f: window_begin", CACurrentMediaTime()*1000000000);
        [sharedConnection sendSinter:sinter];
         */
    }
}

- (void) dealloc {
    [[NSNotificationCenter defaultCenter] removeObserver:self];
    [sharedConnection close];
    
    NSLog(@"Dealloc called");
}

@end
