//
//  CustomOutlineView.h
//  NVRDP
//
//  Created by Syed Masum Billah on 1/30/16.
//  Copyright © 2016 Stony Brook University. All rights reserved.
//

#import <Cocoa/Cocoa.h>
#import "ClientHandler.h"
#import "Model.h"

@interface CustomOutlineView : NSOutlineView <NSOutlineViewDataSource,NSOutlineViewDelegate> {
    int keyPressCount;
    BOOL shouldForward;
    BOOL treeLoaded;
}

@property (strong, nonatomic) Model* root;
@property(weak)  ClientHandler  * sharedConnection;
@property (assign, nonatomic) BOOL shouldSendKeyStrokes;

-(id) initWithFrame:(NSRect)frameRect;
-(id) initWithFrame:(NSRect)frameRect model:(Model*) _model andContainer:(NSView*) container;
@end
