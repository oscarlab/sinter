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
    Header.h
    testReflection

    Created by Syed Masum Billah on 10/14/16.
*/

#import <Foundation/Foundation.h>

#import "Screen.h"
#import "MouseOrCaret.h"
#import "KbdOrAction.h"
#import "Header.h"
#import "Params.h"

@interface Header : NSObject 

@property (nonatomic, assign) NSNumber* service_code;
@property (nonatomic, assign) NSNumber* sub_code;
@property (nonatomic, retain) NSString* timestamp;
@property (nonatomic, retain) NSString* process_id;
@property (nonatomic, retain) Params* params;
//@property (nonatomic, retain) Screen*       screen;
@property (nonatomic, retain) KbdOrAction*  kbd_or_action;
@property (nonatomic, retain) MouseOrCaret* mouse_or_caret;


- (id) init;
- (id) initWithServiceCode:(NSNumber *)service_code;
- (id) initWithServiceCode:(NSNumber *)service_code subCode:(NSNumber *)sub_code processId:(NSString*)process_id parameters:(Params*)params;

- (NSDate *) getNSDate;

+ (NSArray *) getSerializableProperties;

@end
