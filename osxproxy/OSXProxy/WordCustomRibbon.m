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
    WordCustomRibbon.m
    NVRDP

    Created by Tabish Ahmad on 3/22/15.
*/

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
