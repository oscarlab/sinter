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
    CustomOutlineView.h
    NVRDP

    Created by Syed Masum Billah on 1/30/16.
*/

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
