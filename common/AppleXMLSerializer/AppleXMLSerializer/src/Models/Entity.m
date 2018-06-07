//
//  Entity.m
//  testReflection
//
//  Created by Syed Masum Billah on 10/14/16.
//  Copyright © 2016 Syed Masum Billah. All rights reserved.
//

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
        _unique_id = _name = _value = _type = @"";
        _top = _left = _height = _width = _child_count = _states = [NSNumber numberWithInt:0];
    }
    return self;
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
