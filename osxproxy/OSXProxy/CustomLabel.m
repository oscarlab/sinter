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
    CustomTextField.m
    NVRDP

    Created by Syed Masum Billah on 1/22/16.
*/

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
