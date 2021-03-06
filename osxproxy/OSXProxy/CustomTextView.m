/* Copyright (C) 2014--2018 Stony Brook University
   Copyright (C) 2016--2018 The University of North Carolina at Chapel Hill

   This file is part of the Sinter Remote Desktop System.

   Sinter is dual-licensed, available under a commercial license or
   for free subject to the LGPL.  

   Sinter is free software: you can redistribute it and/or modify it
   under the terms of the GNU Lesser General Public License as published by
   the Free Software Foundation, either version 3 of the License, or
   (at your option) any later version.  Sinter is distributed in the
   hope that it will be useful, but WITHOUT ANY WARRANTY; without even
   the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
   PURPOSE.  See the GNU Lesser General Public License for more details.  You
   should have received a copy of the GNU Lesser General Public License along
   with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

/*
    CustomTextView.m
    NVRDP

    Created by Syed Masum Billah on 1/31/16.
*/

#import "CustomTextView.h"
#import "XMLTags.h"

@implementation CustomTextView
@synthesize sharedConnection;
@synthesize process_id;

-(id) initWithFrame:(NSRect)frameRect andConnection:(ClientHandler*) connection{
    self = [super initWithFrame:frameRect];
    if (self) {
        sharedConnection = connection;
        keyPressCount = 1;
    }
    return self;
}


- (void)drawRect:(NSRect)dirtyRect {
    [super drawRect:dirtyRect];
    
    // Drawing code here.
}
//
//-(void)keyDown:(NSEvent *)event {
//    if (event.isARepeat) {
//        keyPressCount++;
//    }
//}
//
//- (void) keyUp:(NSEvent *)theEvent{
//    [self interpretKeyEvents:[NSArray arrayWithObject:theEvent]];
//    keyPressCount = 1;
//}
//
//#pragma mark NSResonponder methods
////- (void)insertNewline:(id)sender{
////    [sharedConnection sendSpecialStroke:@"ENTER" numRepeat:keyPressCount];
////}
//
- (NSRange) getDeleteRange {
    NSRange range = [self selectedRange];
    if (range.length > 0) {
        return range;
    }
    
    int location =  (int)(range.location + range.length) - keyPressCount;
    if (location < 0) {
        location = 0;
    }
    range.location = location;
    range.length = keyPressCount;

    return range;
}
//
- (void)deleteBackward:(id)sender {
    
    //NSLog(@"current position %lu %lu",  [self selectedRange].length , (unsigned long)[self selectedRange].location);
    NSRange range = [self getDeleteRange];
    [self setSelectedRange:range];
    //[sharedConnection sendSpecialStroke:@"BACKSPACE" numRepeat:1];
    [sharedConnection sendKeystrokes:@"{BACKSPACE}" processId:self.process_id targetId:self.identifier];
    //[sharedConnection sendActionMsg:nil targetId:(self.identifier) actionType:STRActionAppendText data:@"{BACKSPACE}"];
    [self delete:nil];
    //[super deleteBackward:sender];
}

- (void)deleteForward:(id)sender{
    //NSLog(@"current position %lu %lu",  [self selectedRange].length , (unsigned long)[self selectedRange].location);
    //[sharedConnection sendSpecialStroke:@"DELETE" numRepeat:keyPressCount];
    [sharedConnection sendSpecialStroke:@"{DELETE}" numRepeat:keyPressCount];
    
    [self setSelectedRange:[self getDeleteRange]];
    [self delete:nil];

}

- (void)keyDown:(NSEvent *)event{
    [self interpretKeyEvents:[NSArray arrayWithObject:event]];
    
    /* https://developer.apple.com/library/archive/documentation/Cocoa/Conceptual/EventOverview/HandlingKeyEvents/HandlingKeyEvents.html */
    /* https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.sendkeys?redirectedfrom=MSDN&view=netframework-4.7.2 */
    if ([event modifierFlags] & NSNumericPadKeyMask) { // arrow keys have this mask
        NSString *theArrow = [event charactersIgnoringModifiers];
        unichar keyChar = 0;
        if ( [theArrow length] == 0 )
            return;            // reject dead keys
        if ( [theArrow length] == 1 ) {
            keyChar = [theArrow characterAtIndex:0];
            if ( keyChar == NSLeftArrowFunctionKey ) {
                [sharedConnection sendKeystrokes:@"{LEFT}" processId:process_id targetId:self.identifier]; //LEFT ARROW key
            }
            if ( keyChar == NSRightArrowFunctionKey ) {
                [sharedConnection sendKeystrokes:@"{RIGHT}" processId:process_id targetId:self.identifier]; //RIGHT ARROW key
            }
            if ( keyChar == NSUpArrowFunctionKey ) {
                [sharedConnection sendKeystrokes:@"{UP}" processId:process_id targetId:self.identifier]; //UP ARROW key
            }
            if ( keyChar == NSDownArrowFunctionKey ) {
                [sharedConnection sendKeystrokes:@"{DOWN}" processId:process_id targetId:self.identifier]; //DOWN ARROW key
            }
        }
    }
}


//- (void)insertText:(id)insertString {
//
//    NSLog(@"current position %lu %lu",  [self selectedRange].length , (unsigned long)[self selectedRange].location);
//    [sharedConnection sendSpecialStroke:insertString numRepeat:keyPressCount];
//    [super insertText:[@"" stringByPaddingToLength:keyPressCount withString: insertString startingAtIndex:0]
//        replacementRange:NSMakeRange([[self  string] length], keyPressCount)];
//}
//


@end
