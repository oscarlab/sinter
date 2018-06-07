//
//  CustomPathControl.m
//  NVRDP
//
//  Created by Syed Masum Billah on 2/17/16.
//  Copyright © 2016 Stony Brook University. All rights reserved.
//

#import "CustomPathControl.h"

@implementation CustomPathControl

@synthesize sharedConnection;
@synthesize root;

-(id) initWithFrame:(NSRect)frameRect{
    self = [super initWithFrame:frameRect];
    if (self) {
        sharedConnection = [ClientHandler getConnection];
        keyPressCount = 1;
        //shouldSendKeyStrokes = NO;
    }
    return self;
}

-(id) initWithFrame:(NSRect)frameRect model:(Model*) _model andContainer:(NSView*) container {
    self = [self initWithFrame:frameRect];
    if (self) {
        root = _model;
        [self setPathStyle:NSPathStylePopUp];
        [self setTarget:self];
        [self setDelegate:(id<NSPathControlDelegate>)self];
        [self setAction:@selector(pathControlAction:)];
        
    }
    // identifier
    [self setIdentifier: root.unique_id];
    //[(id) pathControl isAccessibilityElement:YES];
    [container addSubview:self];
    
    // display method
    [self updatePathControlDisplay];

    return self;
}

- (void) updadeWithRoot:(Model*) _root{
    if (![root.unique_id isEqualToString:_root.unique_id]) {
        NSLog(@"Problem with ID");
    }
    root = _root;
    [self updatePathControlDisplay];
}

- (void)drawRect:(NSRect)dirtyRect {
    [super drawRect:dirtyRect];
    
    // Drawing code here.
}

// adding path segments
- (void) populatePaths{
    NSPathComponentCell *componentCell;
    NSMutableArray *pathComponentArray = [[NSMutableArray alloc] initWithCapacity:root.child_count];
    Model* segment;
    for (int j = 0 ; j < root.child_count ; j++) {
        segment = root.children[j];
        componentCell = [[NSPathComponentCell alloc] init];
        [componentCell setURL:[NSURL URLWithString:segment.unique_id]];
        [componentCell setTitle:segment.name];
        [pathComponentArray addObject:componentCell];
    }
    [self setPathComponentCells:pathComponentArray];
}

- (void) updatePathControlDisplay {
    [self setAccessibilityTitle:root.name];
    [self populatePaths];
    
    NSAccessibilityPostNotification(self, NSAccessibilityTitleChangedNotification);

    // handle overlapping views
    [self setHidden:NO];
    [self setNeedsDisplay:YES];
}


#pragma mark delegates method
- (void)pathControl:(NSPathControl *)pathControl willDisplayOpenPanel:(NSOpenPanel *)openPanel {
    [openPanel setAllowsMultipleSelection:NO];
    [openPanel setCanChooseDirectories:NO];
    [openPanel setCanChooseFiles:NO];
    [openPanel setResolvesAliases:NO];
    [openPanel close];
}

// available in NSPathStylePopUp
- (void)pathControl:(NSPathControl *)pathControl willPopUpMenu:(NSMenu *)menu {
    [menu removeItemAtIndex:0];
    /*
     NSString *title = @"Reveal in Finder";
     NSMenuItem *newItem = [[NSMenuItem alloc] initWithTitle:title action:@selector(pathControlAction:) keyEquivalent:@""];
     [newItem setTarget:self];
     
     [menu addItem:[NSMenuItem separatorItem]];
     [menu addItem:newItem];
     */
}

#pragma mark action handler
- (void) pathControlAction:(id)sender {
    if ([sender isKindOfClass:[NSPathControl class]]) {
        NSPathControl* pathControl = (NSPathControl*) sender;
        NSPathComponentCell *cell = [pathControl clickedPathComponentCell];
        if (cell) {
            NSString* unique_id = [[cell URL] absoluteString];
            [sharedConnection sendActionAt:unique_id];
            //NSLog(@"clicked cell %@", unique_id);
        }
    }
    if ([sender isKindOfClass:[NSMenu class]]) {
        // later
    }
}


@end
