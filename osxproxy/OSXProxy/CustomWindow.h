//
//  CustomWindow.h
//  NVRDP
//
//  Created by Syed Masum Billah on 1/30/16.
//  Copyright © 2016 Stony Brook University. All rights reserved.
//

#import <Cocoa/Cocoa.h>
#import "ClientHandler.h"
#import "KeyMapping.h"

@interface CustomWindow : NSWindow{
    int keyPressCount;
    NSString* _key;
    unichar keyChar;
    NSUInteger flag;
    NSArray * functionKeys;
}

@property(weak)  ClientHandler  * sharedConnection;
@property (retain, strong) NSString* process_id;

@end
