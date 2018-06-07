//
//  CustomTextField.h
//  NVRDP
//
//  Created by Syed Masum Billah on 1/22/16.
//  Copyright © 2016 Stony Brook University. All rights reserved.
//

#import <Cocoa/Cocoa.h>
#import "ClientHandler.h"
#import "KeyMapping.h"

@interface CustomLabel : NSView <NSAccessibilityStaticText>{
    NSString * strValue;
    NSString* label;
    unichar keyChar;
    int keyPressCount;
    
}
- (id) initWithFrame:(NSRect) frameRect andConnection:(ClientHandler *) connection;
- (void) setStrValue:(NSString*) value;
- (void) setLabel:(NSString*) _label;

@property(weak)  ClientHandler  * sharedConnection;

@end
