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
