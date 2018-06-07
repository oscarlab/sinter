//
//  CustomTextField.m
//  NVRDP
//
//  Created by Syed Masum Billah on 1/30/16.
//  Copyright © 2016 Stony Brook University. All rights reserved.
//

#import "CustomTextField.h"

@implementation CustomTextField

- (id) init {
    self = [super init];
    return self;
}

- (void)drawRect:(NSRect)dirtyRect {
    [super drawRect:dirtyRect];
    
    // Drawing code here.
}

- (void)mouseDown:(NSEvent *)theEvent {
    [self sendAction:[self action] to:[self target]];
}

@end
