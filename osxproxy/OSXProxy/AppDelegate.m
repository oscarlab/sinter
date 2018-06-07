//
//  AppDelegate.m
//  NVRDP
//
//  Created by Syed Masum Billah on 7/31/14.
//  Copyright (c) 2014 Stony Brook University. All rights reserved.
//

#import "AppDelegate.h"
#import "CustomWindowController.h"
#import "Model.h"
#import "ControlTypes.h"
#import "XMLTags.h"
#import "Config.h"

#import "ClientHandler.h"

static NSDictionary* serviceCodes;

@implementation AppDelegate


@synthesize window;
@synthesize pid;
@synthesize remoteProcesses;
@synthesize processModel;
@synthesize processTable;
@synthesize remoteWindowControllers;
@synthesize ConnectButton;
@synthesize LoadButton;


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

    [[NSNotificationCenter defaultCenter]
        addObserver:self
        selector:@selector(receivedMessage:)
        name:@"AppDelegate"
        object:nil];

    //shared connection
    sharedConnection = [ClientHandler sharedConnectionWith:[settings objectForKey:@"server_ip"]
                                        andPort:[[settings objectForKey:@"port"] intValue]];
    

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
    if (sharedConnection.isConnected) {
        //NSRunAlertPanel(@"Successfully connected to server.", @"Please press the Load Processes button", @"Ok", nil, nil);
        return;
    }
    
    if(!sharedConnection.isConnected){
        [sharedConnection initForClientSocket];
        //usleep(1000000);
        //NSRunAlertPanel(@"Connection request is sent", @"Please press the Load Processes button next", @"Ok", nil, nil);
    }
}

- (IBAction) disconnect:(id)sender {
    if (sharedConnection.isConnected) {
        [sharedConnection close];
    }
    NSLog(@"received a disconnect message");
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
    
    for (int i = 0 ; i < [windows count] ; i++ ) {
        [self removeWindowWithPID:_pid
            andUniqueId:[[windows[i] rmUiRoot] unique_id]];
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

- (void) receivedMessage:(NSNotification *) notification {
    // ([[notification name] isEqualToString:@"TestNotification"])
    NSDictionary *userInfo = notification.userInfo;
    if(userInfo) {
        Sinter *sinter = [userInfo objectForKey:@"sinter"];
        [self takeActionForXML:sinter];
        NSLog(@"socket client data %i", [sinter.header.service_code intValue]);
    }
}


// MARK: ls (list all remote processes)
- (void) takeActionForXML:(Sinter *) sinter {
//- (void) takeActionForXML:(NSXMLDocument*) xmlDoc withServiceCode:(NSString *)service_code{
    NSNumber * service_code = sinter.header.service_code;
    
    if ([service_code isEqualToNumber: [serviceCodes objectForKey:@"ls_res"]]) {
        NSArray* apps = sinter.applications;
        if (!apps)  return;
        
        remoteProcesses = [[NSMutableArray alloc] initWithCapacity:[apps count]];
        for (Entity * entity in apps){
            Model * remote_process          = [[Model alloc] initWithEntity:entity];
            [remoteProcesses addObject:remote_process];
        }
        [processTable reloadData];
    }
    
    else if ([service_code isEqualToNumber: [serviceCodes objectForKey:@"ls_l_res"]]) {
        NSString * processId = sinter.header.process_id;
        NSString * uniqueId  = sinter.entity.unique_id;
        Entity * entity      = sinter.entity;
        
        if (![entity.type isEqualToString:@"window"]) {
            for(Entity* _entity in entity.children) {
                if([_entity.type isEqualToString:@"window"]){
                    entity = _entity;
                    break;
                }
            }
        }
        
        CustomWindowController *rmWinController = [self getWindowWithPID:processId andUniqueId:uniqueId];
        if (!rmWinController) {
            rmWinController = [[CustomWindowController alloc]
               initWithWindowNibName:@"RemoteWindowController" fromEntity:entity havingProcessID:processId];
            [self addWindow:rmWinController havingPID:processId];
        }
    }
    else if ([service_code isEqualToNumber: [serviceCodes objectForKey:@"window_closed"]]) {
        NSString * processId      = sinter.header.process_id;
        NSString * targetId  = [sinter.header.kbd_or_action target_id];
        // remove
        [self removeWindowWithPID:processId andUniqueId:targetId];
    }
    else if ([service_code isEqualToNumber: [serviceCodes objectForKey:@"kbd"]]) {
        
    }
    else if ([service_code isEqualToNumber: [serviceCodes objectForKey:@"mouse"]]) {
        
    }
    else if ([service_code isEqualToNumber: [serviceCodes objectForKey:@"delta"]]) {
        //int pid = [sinter.header.process_id intValue];
        
    }
    else if ([service_code isEqualToNumber: [serviceCodes objectForKey:@"focus"]]) {
        //int pid = [sinter.header.process_id intValue];
        
    }
    else if ([service_code isEqualToNumber: [serviceCodes objectForKey:@"default_action"]]) {
        //int pid = [sinter.header.process_id intValue];
        
    }
    else {
        
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
        Sinter * sinter = [[Sinter alloc] init];
        Header *header = [[Header alloc] init] ;
        header.service_code = [serviceCodes objectForKey:@"ls_l_req"];
        
        NSLog(@"sending ls:%@", [processModel process_id]);
        header.process_id = [processModel process_id];
        sinter.header = header;
        
        //startTime = CACurrentMediaTime();
        //NSLog(@"[sinter] - :%.0f: window_begin", CACurrentMediaTime()*1000000000);
        [sharedConnection sendSinter:sinter];
    }
}

- (void) dealloc {
    [[NSNotificationCenter defaultCenter] removeObserver:self];
    [sharedConnection close];
    
    NSLog(@"Dealloc called");
}

@end
