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
    Screen.m
    AppleXMLSerializer

   Created by Syed Masum Billah on 10/16/16.
*/ 

#import "Screen.h"
#import "Config.h"

@implementation Screen

static NSArray * props;

+ (void) initialize {
    props = [Config getProperties:NSStringFromClass([self class])];
}

+ (NSArray *) getSerializableProperties {
    return props;
}


-(id) init {
    if ( self = [super init] ) {
        _screen_width = _screen_height = [NSNumber numberWithInt:0];
    }
    return self;
}

-(id) initWithHeight:(int) height andWidth:(int) width {
    if ( self = [super init] ) {
        _screen_height = [NSNumber numberWithInt:height];
        _screen_width  = [NSNumber numberWithInt:width];
    }
    return self;
}

-(void) setScreen_width:(NSString *)screen_width {
    _screen_width = [NSNumber numberWithInt: [screen_width intValue]];
}

-(void) setScreen_height:(NSString *)screen_height {
    _screen_height = [NSNumber numberWithInt: [screen_height intValue]];
}



@end
