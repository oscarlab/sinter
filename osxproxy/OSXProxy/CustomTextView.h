//
//  CustomTextView.h
//  NVRDP
//
//  Created by Syed Masum Billah on 1/31/16.
//  Copyright © 2016 Stony Brook University. All rights reserved.
//

#import <Cocoa/Cocoa.h>
#import "ClientHandler.h"

@interface CustomTextView : NSTextView{
    int keyPressCount;
    unichar keyChar;
}
-(id) initWithFrame:(NSRect)frameRect andConnection:(ClientHandler*) connection;
@property (assign, nonatomic) BOOL shouldSendKeyStrokes;
@property(weak)  ClientHandler  * sharedConnection;

@end
