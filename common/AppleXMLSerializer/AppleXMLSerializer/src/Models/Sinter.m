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

- (id) initWithServiceCode:(NSNumber * ) serviceCode {
    if ( self = [super init] ) {
        _header = [[Header alloc] initWithServiceCode:serviceCode];
    }
    return self;
}


- (id) initWithServiceCode:(NSNumber * ) serviceCode andKbdOrActionWithTarget:(NSString *) targetId andData:(NSString *) data {
    if ( self = [super init] ) {
        _header = [[Header alloc] initWithServiceCode:serviceCode];
        _header.kbd_or_action = [[KbdOrAction alloc] initWithTarget:targetId andData:data];
    }
    return self;
}


- (id) initWithServiceCode:(NSNumber * ) serviceCode andMouseWithX:(int) x andY:(int) y andButton:(int) button {
    if ( self = [super init] ) {
        _header = [[Header alloc] initWithServiceCode:serviceCode ];
        _header.mouse_or_caret = [[MouseOrCaret alloc] initWithX:x andY:y andButton:button ];
    }
    return self;
}


- (id) initWithServiceCode:(NSNumber * ) serviceCode andCaret:(int) location andLength:(int) ending andTarget:(NSString*) targetId {
    if ( self = [super init] ) {
        _header = [[Header alloc] initWithServiceCode:serviceCode ];
        _header.mouse_or_caret = [[MouseOrCaret alloc] initWithCaret:location andLength:ending andTarget:targetId];
    }
    return self;
}

@end
