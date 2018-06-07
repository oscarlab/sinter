//
//  AppDelegate.h
//  NVRDP
//
//  Created by Syed Masum Billah on 7/31/14.
//  Copyright (c) 2014 Stony Brook University. All rights reserved.
//

#import <Cocoa/Cocoa.h>


@class Model;
@class ClientHandler;

@interface AppDelegate : NSObject <NSApplicationDelegate, NSXMLParserDelegate, NSTableViewDataSource, NSTableViewDelegate,NSOutlineViewDataSource> {
    ClientHandler * sharedConnection;
    BOOL isLoaded;
}

@property (weak) IBOutlet NSButton *ConnectButton;
@property (weak) IBOutlet NSButton *LoadButton;


@property (assign) IBOutlet NSWindow *window;
// array of dictionary: <process_id, window_id1, window_id2, window_id3> 
@property (nonatomic, strong) NSMutableDictionary *remoteWindowControllers;

@property (nonatomic, strong) NSMutableArray *remoteProcesses;
@property (nonatomic, strong) Model *processModel;
@property (nonatomic, strong) NSString* pid;

@property (assign) IBOutlet NSTableView *processTable;

- (void) selectRemoteProcess:(id) process_id;
- (IBAction) connect:(id) sender;
- (IBAction) fetchRemoteProcesses:(id) sender;
- (IBAction) disconnect:(id) sender;

#pragma mark window-cleaning functions
- (void) removeWindowWithPID:(NSString*) _pid andUniqueId:(NSString*) _uniqueId;
- (void) removeAllWindowsWithPID: (NSString*) _pid ;
- (void) removeAllWindows;

@end
