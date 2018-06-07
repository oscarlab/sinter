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
    Sinter.m  
    testReflection

    Created by Syed Masum Billah on 10/14/16.
*/

#import "Sinter.h"
#import "Config.h"

@implementation Sinter

static NSArray * props;

+ (void) initialize {
    props = [Config getProperties:NSStringFromClass([self class])];
}

+ (NSArray *) getSerializableProperties {
    return props;
}

- (id) init {
    if ( self = [super init] ) {
//        _header = [[Header alloc] init];
//        _entity = [[Entity alloc] init];
//        _updates = [[NSMutableArray alloc] init];
//        _applications = [[NSMutableArray alloc] init];
    }
    return self;
}

- (id) initWithEntity {
    if ( self = [super init] ) {
        _header = [[Header alloc] init];
        _entity = [[Entity alloc] init];
    }
    return self;
}

- (id) initWithApplications {
    if ( self = [super init] ) {
        _header = [[Header alloc] init];
        _applications = [[NSMutableArray alloc] init];
    }
    return self;
}

- (id) initWithUpdates {
    if ( self = [super init] ) {
        _header = [[Header alloc] init];
        _applications = [[NSMutableArray alloc] init];
    }
    return self;
}

- (id) initWithServiceCode:(NSNumber * ) serviceCode {
    if ( self = [super init] ) {
        _header = [[Header alloc] initWithServiceCode:serviceCode];
    }
    return self;
}


- (id) initWithServiceCode:(NSNumber * ) serviceCode andKbdOrActionWithTarget:(NSString *) targetId andData:(NSString *) data {
    if ( self = [super init] ) {
        _header = [[Header alloc] initWithServiceCode:serviceCode];
        _header.kbd_or_action = [[KbdOrAction alloc] initWithTarget:targetId andData:data];
    }
    return self;
}


- (id) initWithServiceCode:(NSNumber * ) serviceCode andMouseWithX:(int) x andY:(int) y andButton:(int) button {
    if ( self = [super init] ) {
        _header = [[Header alloc] initWithServiceCode:serviceCode ];
        _header.mouse_or_caret = [[MouseOrCaret alloc] initWithX:x andY:y andButton:button ];
    }
    return self;
}


- (id) initWithServiceCode:(NSNumber * ) serviceCode andCaret:(int) location andLength:(int) ending andTarget:(NSString*) targetId {
    if ( self = [super init] ) {
        _header = [[Header alloc] initWithServiceCode:serviceCode ];
        _header.mouse_or_caret = [[MouseOrCaret alloc] initWithCaret:location andLength:ending andTarget:targetId];
    }
    return self;
}

@end
