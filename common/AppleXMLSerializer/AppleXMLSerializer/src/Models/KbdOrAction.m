//
//  Action.m
//  AppleXMLSerializer
//
//  Created by Syed Masum Billah on 10/16/16.
//  Copyright © 2016 Syed Masum Billah. All rights reserved.
//

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

- (id) initWithTarget:(NSString*) target_id andData:(NSString*) data {
    if ( self = [super init] ) {
        _target_id    = [NSString stringWithString:target_id];
        _generic_data = [NSString stringWithString:data];
    }
    return self;
}



@end
