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
    Entity.m
    testReflection

    Created by Syed Masum Billah on 10/14/16.
*/

#import "Entity.h"
#import "Config.h"


@implementation Entity

static NSArray * props;

+ (void) initialize {
    props = [Config getProperties:NSStringFromClass([self class])];
}

+ (NSArray *) getSerializableProperties {
    return props;
}

- (id) init {
    if ( self = [super init] ) {
        _unique_id = _name = _value = _type = _raw_type = @"";
        _top = _left = _height = _width = _child_count = _states = [NSNumber numberWithInt:0];
    }
    return self;
}


-(void) setName:(NSString *)name {
    _name = [NSString stringWithString:name];
}

-(void) setValue:(NSString *)value {
    _value = [NSString stringWithString:value];
}

-(void) setType:(NSString *)type {
    _type = [NSString stringWithString:type];
}

-(void) setUnique_id:(NSString *)unique_id {
    _unique_id = [NSString stringWithString:unique_id];
}

-(void) setProcess_id:(NSString *)process_id {
    _process_id = [NSString stringWithString:process_id];

}

-(void) setStates:(NSString *) states {
    _states = [NSNumber numberWithUnsignedInteger:[states intValue]];
}

-(void) setTop:(NSString *) top {
    _top = [NSNumber numberWithInt: [top intValue]];
}

-(void) setLeft:(NSString *)left {
    _left = [NSNumber numberWithInt: [left intValue]];
}

-(void) setHeight:(NSString *)height {
    _height = [NSNumber numberWithInt: [height intValue]];
}

-(void) setWidth:(NSString *)width {
    _width = [NSNumber numberWithInt: [width intValue]];
}


@end
