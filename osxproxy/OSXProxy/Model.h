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
    RemoteControl.h
    Hello

    Created by Syed Masum Billah on 7/12/14.
*/

#import <Foundation/Foundation.h>
#import "Sinter.h"
#import "Entity.h"
#import "ControlTypes.h"


@interface Model : NSObject <NSCopying>{
    
}

@property (nonatomic, strong) NSString* process_id;
@property (nonatomic, strong) NSString* parent_id;

@property (nonatomic, strong) NSString* unique_id;
@property (nonatomic, strong) NSString* name;
@property (nonatomic, strong) NSString* type;
@property (nonatomic, strong) NSString* value;

//children
@property (nonatomic, strong) NSMutableArray* children;

//optional: to hold custom user data
@property (nonatomic, strong) NSMutableDictionary* user_data;

// atomic properties
@property (assign) unsigned long states; // a bit-mask of states
@property (assign) int version; // 0:new 1:updated
@property (assign) int top, left, width, height, child_count;
@property (assign) int next_sibling, prev_sibling;
@property (assign) BOOL isDuplicate;
// parent
@property (weak) Model* parent;

- (BOOL) isEqualToUI:(id)object;
- (NSString*) toString; //seialize
- (void) printStates; // show states

-(id) initWithEntity:(Entity*) entity;
@end
