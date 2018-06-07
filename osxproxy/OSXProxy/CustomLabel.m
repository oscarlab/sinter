//
//  CustomTextField.m
//  NVRDP
//
//  Created by Syed Masum Billah on 1/22/16.
//  Copyright © 2016 Stony Brook University. All rights reserved.
//

#import "CustomLabel.h"

@implementation CustomLabel
@synthesize sharedConnection;

-(id) initWithFrame:(NSRect)frameRect andConnection:(ClientHandler *) connection{
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
    NSRect bounds = self.bounds;
    NSDictionary *textAttributes = @{ NSFontAttributeName : [NSFont fontWithName:@"Andale Mono" size:11.0f],
                                      NSForegroundColorAttributeName : [NSColor blackColor] };
    [strValue drawInRect:bounds withAttributes:textAttributes];
    
    // Draw the focus ring
    BOOL isFirstResponder = [[[NSApp mainWindow] firstResponder] isEqual:self];
    if ( isFirstResponder ) {
        [NSGraphicsContext saveGraphicsState];
        NSSetFocusRingStyle(NSFocusRingOnly);
        [[NSBezierPath bezierPathWithRect:bounds] fill];
        [NSGraphicsContext restoreGraphicsState];
    }
}

- (BOOL)acceptsFirstResponder {
    return YES;
}

- (BOOL)becomeFirstResponder {
    BOOL didBecomeFirstResponder = [super becomeFirstResponder];
    [self setNeedsDisplay:YES];
    return didBecomeFirstResponder;
}

- (BOOL)resignFirstResponder {
    BOOL didResignFirstResponder = [super resignFirstResponder];
    [self setNeedsDisplay:YES];
    return didResignFirstResponder;
}

- (void) speakString:(NSString *)stringToSpeak fromFocusedUIElement:(id)object {
    NSDictionary *announcementInfo = [[NSDictionary alloc] initWithObjectsAndKeys:
                                      stringToSpeak, NSAccessibilityAnnouncementKey,
                                      @"High", NSAccessibilityPriorityKey, nil];
    [[self window] makeFirstResponder:self];
    NSAccessibilityPostNotificationWithUserInfo(object,
        NSAccessibilityAnnouncementRequestedNotification, announcementInfo);
    
}

- (void) setLabel:(NSString*) _label{
    label = _label;
}

- (void) setStrValue:(NSString*) value{
    strValue = value;
    NSResponder * prev = [[self window] firstResponder];
    [self speakString:strValue fromFocusedUIElement:self];
//    NSAccessibilityPostNotification(self, NSAccessibilityValueChangedNotification);
    [super setNeedsDisplay:YES];
    [[self window] makeFirstResponder:prev];
}

#pragma mark Accessibility
- (NSString *)accessibilityValue{
    return strValue;
}

- (NSString *)accessibilityLabel{
    return label;
}


//- (nullable NSAttributedString *)accessibilityAttributedStringForRange:(NSRange)range;
//- (NSRange)accessibilityVisibleCharacterRange {
//    return NSMakeRange(0, (uint)[label length]);
//};

-(void)keyDown:(NSEvent *)event {
    if (event.isARepeat) {
        keyPressCount++;
    }
}

- (void) keyUp:(NSEvent *)theEvent{
    [self interpretKeyEvents:[NSArray arrayWithObject:theEvent]];
    keyPressCount = 1;
}

- (void)insertNewline:(id)sender{
    [sharedConnection sendSpecialStroke:@"{ENTER}" numRepeat:keyPressCount];
}

- (void)deleteBackward:(id)sender {
    [sharedConnection sendSpecialStroke:@"{BACKSPACE}" numRepeat:keyPressCount];
}

- (void)deleteForward:(id)sender{
    [sharedConnection sendSpecialStroke:@"{DELETE}" numRepeat:keyPressCount];
}

- (void)insertText:(id)insertString {
    [sharedConnection sendSpecialStroke:insertString numRepeat:keyPressCount];
}


@end
