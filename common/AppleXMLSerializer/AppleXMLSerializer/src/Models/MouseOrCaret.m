//
//  Mouse.m
//  AppleXMLSerializer
//
//  Created by Syed Masum Billah on 10/16/16.
//  Copyright © 2016 Syed Masum Billah. All rights reserved.
//

#import "MouseOrCaret.h"
#import "Config.h"

@implementation MouseOrCaret

static NSArray * props;

+ (void) initialize {
    props = [Config getProperties:NSStringFromClass([self class])];
}

+ (NSArray *) getSerializableProperties {
    return props;
}

- (id) initWithX:(int) x andY:(int) y andButton:(int) button {
    if ( self = [super init] ) {
        _x_or_starting  = [NSNumber numberWithInt:x];
        _y_or_ending    = [NSNumber numberWithInt:y];
        _button_type    = [NSNumber numberWithInt:button];
    }
    return self;
}

- (id) initWithCaret:(int) location andLength:(int) ending andTarget:(NSString*) target_id {
    if ( self = [super init] ) {
        _x_or_starting  = [NSNumber numberWithInt:location];
        _y_or_ending    = [NSNumber numberWithInt:ending];
        _target_id      = [NSString stringWithString:target_id];
    }
    return self;
}


-(void) setX_or_starting:(NSString *)x_or_starting {
    _x_or_starting = [NSNumber numberWithInt: [x_or_starting intValue]];
}

-(void) setY_or_ending:(NSString *)y_or_ending {
    _y_or_ending = [NSNumber numberWithInt: [y_or_ending intValue]];
}

-(void) setButton_type:(NSString *)button_type {
    _button_type = [NSNumber numberWithInt: [button_type intValue]];
}


@end
