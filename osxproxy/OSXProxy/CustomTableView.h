//
//  CustomTableView.h
//  NVRDP
//
//  Created by Syed Masum Billah on 1/30/16.
//  Copyright © 2016 Stony Brook University. All rights reserved.
//

#import <Cocoa/Cocoa.h>
#import "ClientHandler.h"
#import "Model.h"

@interface CustomTableView : NSTableView < NSTableViewDelegate, NSTableViewDataSource>{
    int keyPressCount;
    BOOL shouldForward;
    BOOL hasHeader;
    BOOL listLoaded;
}

@property(weak)  ClientHandler  * sharedConnection;
@property (strong, nonatomic) Model* root;
@property (strong, nonatomic) Model* header;

-(id) initWithFrame:(NSRect)frameRect ;
-(id) initWithFrame:(NSRect)frameRect Model:(Model*) _root header:(Model*) _header andContainer:(NSView*) container;
- (void) updateWithRoot:(Model*) _root andHeader:(Model*) _header;
@property (assign, nonatomic) BOOL shouldSendKeyStrokes;
@end
