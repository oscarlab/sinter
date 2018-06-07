//
//  Sinter.m
//  testReflection
//
//  Created by Syed Masum Billah on 10/14/16.
//  Copyright © 2016 Syed Masum Billah. All rights reserved.
//

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

@end
