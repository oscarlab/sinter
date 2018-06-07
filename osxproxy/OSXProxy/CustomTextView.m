//
//  CustomTextView.m
//  NVRDP
//
//  Created by Syed Masum Billah on 1/31/16.
//  Copyright © 2016 Stony Brook University. All rights reserved.
//

#import "CustomTextView.h"

@implementation CustomTextView
@synthesize sharedConnection;

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
    [sharedConnection sendSpecialStroke:@"{BACKSPACE}" numRepeat:1];
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
//
////
//- (void)insertText:(id)insertString {
//
//    NSLog(@"current position %lu %lu",  [self selectedRange].length , (unsigned long)[self selectedRange].location);
//    [sharedConnection sendSpecialStroke:insertString numRepeat:keyPressCount];
//    [super insertText:[@"" stringByPaddingToLength:keyPressCount withString: insertString startingAtIndex:0]
//        replacementRange:NSMakeRange([[self  string] length], keyPressCount)];
//}
//


@end
