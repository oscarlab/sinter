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
    Mouse.m
    AppleXMLSerializer

    Created by Syed Masum Billah on 10/16/16.
*/

#import "MouseOrCaret.h"
#import "Config.h"

@implementation MouseOrCaret

static NSArray * props;

+ (void) initialize {
    props = [Config getProperties:NSStringFromClass([self class])];
}

+ (NSArray *) getSerializableProperties {
    return props;
}

- (id) initWithX:(int) x andY:(int) y andButton:(int) button {
    if ( self = [super init] ) {
        _x_or_starting  = [NSNumber numberWithInt:x];
        _y_or_ending    = [NSNumber numberWithInt:y];
        _button_type    = [NSNumber numberWithInt:button];
    }
    return self;
}

- (id) initWithCaret:(int) location andLength:(int) ending andTarget:(NSString*) target_id {
    if ( self = [super init] ) {
        _x_or_starting  = [NSNumber numberWithInt:location];
        _y_or_ending    = [NSNumber numberWithInt:ending];
        _target_id      = [NSString stringWithString:target_id];
    }
    return self;
}


-(void) setX_or_starting:(NSString *)x_or_starting {
    _x_or_starting = [NSNumber numberWithInt: [x_or_starting intValue]];
}

-(void) setY_or_ending:(NSString *)y_or_ending {
    _y_or_ending = [NSNumber numberWithInt: [y_or_ending intValue]];
}

-(void) setButton_type:(NSString *)button_type {
    _button_type = [NSNumber numberWithInt: [button_type intValue]];
}


@end
