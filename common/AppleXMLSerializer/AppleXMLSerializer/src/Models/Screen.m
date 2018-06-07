//
//  Screen.m
//  AppleXMLSerializer
//
//  Created by Syed Masum Billah on 10/16/16.
//  Copyright © 2016 Syed Masum Billah. All rights reserved.
//

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
