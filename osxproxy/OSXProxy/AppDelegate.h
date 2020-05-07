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
    AppDelegate.h
    NVRDP

    Created by Syed Masum Billah on 7/31/14.
*/

#import <Cocoa/Cocoa.h>
#import "Sinter.h"


@class Model;
@class ClientHandler;

@interface AppDelegate : NSObject <NSApplicationDelegate, NSXMLParserDelegate, NSTableViewDataSource, NSTableViewDelegate,NSOutlineViewDataSource> {
    ClientHandler * sharedConnection;
    BOOL isLoaded;
}

@property (weak) IBOutlet NSButton *ConnectButton;
@property (weak) IBOutlet NSButton *LoadButton;
@property (weak) IBOutlet NSButton *DisconnectButton;
@property (weak) IBOutlet NSTextField *ServerIPTextField;
@property (weak) IBOutlet NSTextField *ServerPortTextField;
@property (weak) IBOutlet NSTextField *PasscodeTextField;



@property (assign) IBOutlet NSWindow *window;
// array of dictionary: <process_id, window_id1, window_id2, window_id3> 
@property (nonatomic, strong) NSMutableDictionary *remoteWindowControllers;

@property (nonatomic, strong) NSMutableArray *remoteProcesses;
@property (nonatomic, strong) Model *processModel;
@property (nonatomic, strong) NSString* pid;

@property (assign) IBOutlet NSTableView *processTable;

- (void) selectRemoteProcess:(id) process_id;
- (void) disconnect;
- (void) takeActionForXML:(Sinter *) sinter;
- (IBAction) connect:(id) sender;
- (IBAction) fetchRemoteProcesses:(id) sender;
- (IBAction) disconnectButtonClicked:(id) sender;


#pragma mark window-cleaning functions
- (void) removeWindowWithPID:(NSString*) _pid andUniqueId:(NSString*) _uniqueId;
- (void) removeAllWindowsWithPID: (NSString*) _pid ;
- (void) removeAllWindows;

@end
