//
//  WordCustomRibbon.m
//  NVRDP
//
//  Created by Tabish on 3/22/15.
//  Copyright (c) 2015 Stony Brook University. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "WordCustomRibbon.h"

@implementation WordCustomRibbon


- (id)initWithWindow:(NSWindow *)window
{
    self = [super initWithWindow:window];
    if (self) {
        // Initialization code here.
    }
    return self;
}

- (id) initWithWindowNibName:(NSString *)windowNibName
{
    self = [super initWithWindowNibName:windowNibName];
    if (self)
    {
        NSRect frame = [self.window frame];
        frame.size.height = 100;
        frame.size.width = 100;
        //set window frame
        [self.window setFrame:frame display:TRUE ];

        //now create cocoa UIs
        [self.window autorecalculatesKeyViewLoop];
        [self.window setAutorecalculatesKeyViewLoop:YES];
        //redraw
        [[self.window contentView] setNeedsDisplay:YES];
    }
    return self;
}

@end