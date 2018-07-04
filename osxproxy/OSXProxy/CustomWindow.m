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
    CustomWindow.m 
    NVRDP

    Created by Syed Masum Billah on 1/30/16.
*/

#import "CustomWindow.h"

@implementation CustomWindow
@synthesize sharedConnection;
@synthesize process_id;

-(id) init {
    NSRect frame = NSMakeRect(0, 0, 10, 10);
    self = [super initWithContentRect:frame
                   styleMask:NSTitledWindowMask|NSClosableWindowMask|NSResizableWindowMask
                   backing:NSBackingStoreBuffered
                   defer:NO];
    if (self) {
        [self center];
        keyPressCount = 1;

        return self;
    }
    return nil;
}


-(BOOL)performKeyEquivalent:(NSEvent *)theEvent{
    NSString* key = [theEvent charactersIgnoringModifiers];
    if (![key length])
        return NO;
    
    keyChar = [key characterAtIndex:0];
    flag = [NSEvent modifierFlags];
    
    // if control, alt, command keys are pressed
    if( flag & NSControlKeyMask   || //CONTROL
        flag & NSAlternateKeyMask || //ALT
        flag & NSCommandKeyMask ){   //Command

        if(keyChar == 'o'){// Cmd+O
            [sharedConnection sendKeystorkesAt:nil strokes:@"{ENTER}"];
            return YES;
        }
        
        _key = [KeyMapping keyStringFormKeyCodeSimplified:theEvent.keyCode];//i.e. {TAB%@}
        if (_key) {
            key = _key;
        }
        
        if(flag & NSControlKeyMask){
            [sharedConnection sendKeystorkesAt:nil
               strokes:[NSString stringWithFormat: @"^(%@)", key]];
            return YES;
        }
        if(flag & NSAlternateKeyMask){
            [sharedConnection sendKeystorkesAt:nil
               strokes:[NSString stringWithFormat: @"%%(%@)", key]];
            return YES;
        }
    }
    // if a key is function key, then it is also considered as shortcut
    switch (keyChar) {
        case NSF1FunctionKey:
        case NSF2FunctionKey:
        case NSF3FunctionKey:
        case NSF4FunctionKey:
        case NSF5FunctionKey:
        case NSF6FunctionKey:
        case NSF7FunctionKey:
        case NSF8FunctionKey:
        case NSF9FunctionKey:
        case NSF10FunctionKey:
        case NSF11FunctionKey:
        case NSF12FunctionKey:{
            _key = [KeyMapping keyStringFormKeyCodeSimplified:theEvent.keyCode];//i.e. {TAB%@}
            if (_key) {
                [sharedConnection sendKeystorkesAt:nil strokes:_key];
                return YES;
            }
        
        } break;
            
        default:
            break;
    }
    
    // return default
    return NO;
}

@end
