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
    Action.m
    AppleXMLSerializer

    Created by Syed Masum Billah on 10/16/16.
*/

#import "KbdOrAction.h"
#import "Config.h"

@implementation KbdOrAction

static NSArray * props;

+ (void) initialize {
    props = [Config getProperties:NSStringFromClass([self class])];
}

+ (NSArray *) getSerializableProperties {
    return props;
}

- (id) initWithTarget:(NSString*) target_id {
    if ( self = [super init] ) {
        _target_id    = [NSString stringWithString:target_id];
    }
    return self;
}

- (id) initWithTarget:(NSString*) target_id andData:(NSString*) data {
    if ( self = [super init] ) {
        _target_id    = [NSString stringWithString:target_id];
        _generic_data = [NSString stringWithString:data];
    }
    return self;
}



@end
