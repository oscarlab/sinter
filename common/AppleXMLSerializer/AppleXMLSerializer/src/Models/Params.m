//
//  Params.m
//  OSXProxy
//
//  Created by Erica Fu on 2/28/19.
//  Copyright © 2019 Stony Brook University. All rights reserved.
//

#import "Params.h"
#import "Config.h"

@implementation Params

static NSArray * props;

+ (void) initialize {
    props = [Config getProperties:NSStringFromClass([self class])];
}

+ (NSArray *) getSerializableProperties {
    return props;
}

- (id) init {
    if ( self = [super init] ) {
    }
    return self;
}
@end
