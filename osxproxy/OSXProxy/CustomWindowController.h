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
    RemoteWindowController.h
    RemoteProcess
  
    Created by Syed Masum Billah on 7/21/14.
*/

#import <Cocoa/Cocoa.h>

#import "DrawUI.h"
#import "AppDelegate.h"
#import "CustomWindow.h"
#import "Sinter.h"
#import "ClientHandler.h"

@class RemoteProcess;

@class Model;

#define UPDATED 1
#define STALE_OR_NEW 0


@interface CustomWindowController : NSWindowController < NSXMLParserDelegate, NSTextViewDelegate,  NSWindowDelegate, NSToolbarDelegate, NSMenuDelegate  >{
    
    Model * rmUiRoot; /* */
    DrawUI * renderer;
    NSResponder* currentResponder;
    
    NSCharacterSet* charsetLetter;
    NSCharacterSet* charsetNumber;
    NSArray* charsetCustom;
    
    // junks
    NSMutableDictionary * customToolbarTable;
    NSMutableDictionary *items;
    NSToolbar *toolbar;
    NSView *toolbarView;
    NSMenu *mainMenu;
    NSMenu *remoteMenu;
    
    NSTabView *tabView;
    NSView *tabCView;
    int index;
    int tag;
    NSRange range;
    bool isLoaded;
        
    int rm_screen_h, rm_screen_w;
    int remote_process_screen_height, remote_process_screen_width;
    int keyPressCount;
}
@property (weak) ClientHandler  * sharedConnection;
@property (nonatomic, strong) NSString *process_id;
@property (nonatomic, strong) NSArray *service_codes;
@property (nonatomic, strong) Entity *xmlDOM;


@property (nonatomic, strong) NSString *focussedID;
@property (nonatomic, strong) NSView *focussedView;
@property (assign) BOOL isChild;

@property (nonatomic, strong) RemoteProcess * remoteProcess;
@property (nonatomic, strong) Model * rmUiRoot;
@property (nonatomic, strong) CustomWindowController* childWindow;


@property (assign) NSRect localWinFrame;

@property (nonatomic, strong) NSMutableDictionary * idTable;
@property (nonatomic, strong) NSMutableDictionary * idToUITable;
@property (nonatomic, strong) NSMutableDictionary * screenMapTable;

@property (nonatomic, strong) NSString * keystrokes;
@property (assign) float rm_screen_ratio_x,rm_screen_ratio_y;
@property (assign) float screen_ratio_host_x,screen_ratio_host_y;
@property  (assign) int prev_selected_menu_index, prev_menu_x, prev_menu_y,prev_action_x,prev_action_y;

@property (assign) BOOL shouldClose;
@property (assign) BOOL closeCancelled;

- (id) initWithWindowNibName:(NSString *)windowNibName fromEntity:(Entity *) entity havingProcessID:(NSString*) processId moreEntities:(NSArray *)entities;

- (NSView *) renderDOM:(Model *) current anchor:(id) anchor;

- (void) comeIntoView;
@end
