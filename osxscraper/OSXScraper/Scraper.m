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
    Scraper.m
    OSXScrapper

    Created by Syed Masum Billah on 10/19/16.
*/

#import "Scraper.h"
#include "Sinter.h"
#include "KeyMapping.h"
#include "Config.h"
#include "AccAPI.h"
#import "XMLTags.h"
#import "ScraperServer.h"


//static NSDictionary* serviceCodes;

@implementation Scraper
@synthesize appCache;

+ (void) initialize {
    serviceCodes = [Config getServiceCodes];
}

Scraper * refToSelf;

- (id) initWithId:(int) identifier andClientHandler:(ClientHandler *) clientHandler {
    self = [super init];
    if (self) {
        refToSelf = self;
        [self setIdentifier:identifier];
        [self setClientHandler:clientHandler];
        _isPasscodeVerified = false;
        
        appCache = [[NSMutableDictionary alloc] init];
        
        [self checkAccessibilityAPI];
        // add notification
        [[NSNotificationCenter defaultCenter]
            addObserver:self
            selector:@selector(receivedMessage:)
            name:[NSString stringWithFormat:@"Client_%i_Notification", identifier]
            object:nil];
    }
    return self;
}

- (void) receivedMessage:(NSNotification *) notification {
    // [notification name] should always be @"Client_%i_Notification"
    NSDictionary *userInfo = notification.userInfo;
    if(userInfo) {
        Sinter *sinter = [userInfo objectForKey:@"sinter"];
        NSNumber * service_code = sinter.header.service_code;
        if(_isPasscodeVerified == true || [service_code isEqualToNumber: [serviceCodes objectForKey:STRVerifyPasscode]]){
            [self execute:sinter];
        }
        else{
            //NSLog(@"ignore msgs before passcode verified");
        }
    }
}


- (void) execute: (Sinter *) cmdSinter {
    NSNumber * service_code = cmdSinter.header.service_code;
    Sinter * sinter = nil;
    if ([service_code isEqualToNumber: [serviceCodes objectForKey:STRVerifyPasscode]]) {
        int client_passcode = [cmdSinter.header.params.data1 intValue];
        if(client_passcode == gPasscode){
            [_clientHandler sendPasscodeVerifyRes:true];
            _isPasscodeVerified = true;
        }
        else{
            [_clientHandler sendPasscodeVerifyRes:false];
            [_clientHandler close];
        }
    }
    else if ([service_code isEqualToNumber: [serviceCodes objectForKey:STRLsReq]]) {
        sinter = [AccAPI getListOfApplications];
    
    }
    else if ([service_code isEqualToNumber: [serviceCodes objectForKey:STRLsLongReq]]) {
        int pid = [cmdSinter.header.process_id intValue];
        sinter = [self handleLSRequestwithPid:pid];
    }
    else if ([service_code isEqualToNumber: [serviceCodes objectForKey:STRKeyboard]]) {
        int pid = [cmdSinter.header.process_id intValue];
        if (cmdSinter.header.kbd_or_action) {
            [self handleKeyboardInput:pid andValue: cmdSinter.header.kbd_or_action.generic_data];
        }
    }
    else if ([service_code isEqualToNumber: [serviceCodes objectForKey:STRMouse]]) {
        int pid = [cmdSinter.header.process_id intValue];
        MouseOrCaret * mouse = cmdSinter.header.mouse_or_caret;
        if (mouse) {
            [self handleMouseClick:pid andX:mouse.x_or_starting andY:mouse.y_or_ending andButton:mouse.button_type];
        }
    }
    else if ([service_code isEqualToNumber: [serviceCodes objectForKey:STRDelta]]) {
        //int pid = [sinter.header.process_id intValue];
        
    }
    else if ([service_code isEqualToNumber: [serviceCodes objectForKey:STRActionChangeFocus]]) {
        //int pid = [sinter.header.process_id intValue];
        
    }
    else if ([service_code isEqualToNumber: [serviceCodes objectForKey:STRActionDefault]]) {
        //int pid = [sinter.header.process_id intValue];
    
    }
    else {
    
    }
    
    // send sinter response
    if (sinter) {
        [_clientHandler sendSinter:sinter];
    }
}


- (void) handleMouseClick:(int) pid andX:(NSNumber *) x andY:(NSNumber *) y andButton:(NSNumber *) button {
    CGEventRef event1, event2;
    
    NSRunningApplication* app = [NSRunningApplication runningApplicationWithProcessIdentifier: pid];
    if ([app activateWithOptions:(NSApplicationActivateAllWindows | NSApplicationActivateIgnoringOtherApps)]) {
        while (![app isActive]) {
            [NSThread sleepForTimeInterval:0.1];
            app = [NSRunningApplication runningApplicationWithProcessIdentifier: pid];
        }
    }
    
    int button_code = ![button intValue] ? kCGMouseButtonLeft : kCGMouseButtonRight;
    //  button down
    event1 = CGEventCreateMouseEvent( NULL, button == 0 ?
                     kCGEventLeftMouseDown: kCGEventRightMouseDown,
                     CGPointMake([x intValue], [y intValue]),
                     button_code);
    //  button up
    event2 = CGEventCreateMouseEvent( NULL, button == 0?
                     kCGEventLeftMouseUp: kCGEventRightMouseUp,
                     CGPointMake([x intValue], [y intValue]),
                     button_code);
    
    CGEventPost(kCGHIDEventTap, event1);
    CGEventPost(kCGHIDEventTap, event2);
    
    // Release the events
    CFRelease(event1);
    CFRelease(event2);
}

- (void) handleKeyboardInput:(int) pid andValue: (NSString *) value {
    CGEventRef event1, event2;
    CGKeyCode cg_key_code = [KeyMapping keyCodeFormKeyString: value];
    
    NSRunningApplication* app = [NSRunningApplication runningApplicationWithProcessIdentifier: pid];
    if ([app activateWithOptions:(NSApplicationActivateAllWindows | NSApplicationActivateIgnoringOtherApps)]) {
        while (![app isActive]) {
            [NSThread sleepForTimeInterval:0.1];
            app = [NSRunningApplication
                   runningApplicationWithProcessIdentifier: pid];
        }
    }
    
    event1 = CGEventCreateKeyboardEvent (NULL, (CGKeyCode) cg_key_code, true);
    event2 = CGEventCreateKeyboardEvent (NULL, (CGKeyCode) cg_key_code, false);
    
    CGEventPost(kCGHIDEventTap, event1);
    CGEventPost(kCGHIDEventTap, event2);
    
    // Release the events
    CFRelease(event1);
    CFRelease(event2);
}

- (Sinter *) handleLSRequestwithPid: (pid_t) pid {
    NSMutableDictionary* cache_pid = [appCache objectForKey:[NSNumber numberWithInt:pid]];
    if(!cache_pid){
        cache_pid = [[NSMutableDictionary alloc] init];
        [appCache setObject:cache_pid forKey:[NSNumber numberWithInt:pid]];
    }
    else{
        [cache_pid removeAllObjects];
    }
    
    AXUIElementRef app_ref;
    Sinter * sinter = [AccAPI getDomOf:pid andReturnRef:&app_ref withCache:cache_pid];
    if (sinter) {
        //[self registerObserverFor:pid forElementRef:app_ref];
    }
    return sinter;
}


// notice: this is C-code, notificationName are defined in NSAccessibility.h header
void structureChangeHandler(AXObserverRef obsever, AXUIElementRef element, CFStringRef notificationName, void *contextData) {
    pid_t pid;
    if( AXUIElementGetPid(element, &pid) != kAXErrorSuccess){
        NSLog(@"Observer registration failed");
        return ;
    }
    
    NSMutableDictionary * pid_cache = [[refToSelf appCache] objectForKey:[NSNumber numberWithInt:pid]] ;
    Sinter * sinter =  [AccAPI getDeltaAt:element havingPID:pid andUpdateType:(__bridge NSString *)(notificationName) withCache:pid_cache];
    if (sinter) {
        [[refToSelf clientHandler] sendSinter:sinter];
    }
}


- (BOOL) registerObserverFor:(pid_t) pid forElementRef:(AXUIElementRef) appRef {
    AXObserverRef observer_ref;
    if (AXObserverCreate(pid, structureChangeHandler, &observer_ref) != kAXErrorSuccess) {
        NSLog(@"error creating observer");
        return NO;
    }
    
    AXError code;
    // register kAXValueChangedNotification
    if( (code = AXObserverAddNotification(observer_ref, appRef, kAXValueChangedNotification, (__bridge void *)(self)))!=kAXErrorSuccess) {
        [AccAPI printObserverStatus:code];
    }

    // register kAXFocusedUIElementChangedNotification
    if( (code = AXObserverAddNotification(observer_ref, appRef, kAXFocusedUIElementChangedNotification, (__bridge void *)(self)))!=kAXErrorSuccess) {
        [AccAPI printObserverStatus:code];
    }
    
    // register kAXSelectedChildrenChangedNotification
    if( (code = AXObserverAddNotification(observer_ref, appRef, kAXSelectedChildrenChangedNotification, (__bridge void *)(self)))!=kAXErrorSuccess) {
        [AccAPI printObserverStatus:code];
    }
    
    // run thread
    CFRunLoopAddSource( [[NSRunLoop currentRunLoop] getCFRunLoop],
                       AXObserverGetRunLoopSource(observer_ref),
                       kCFRunLoopDefaultMode);
    return YES;
}


- (bool) checkAccessibilityAPI {
    NSDictionary *options = @{(__bridge id) kAXTrustedCheckOptionPrompt: @YES };
    if(!AXIsProcessTrustedWithOptions((__bridge CFDictionaryRef)options)){
        NSLog(@"Accessibility API not enabled");
    }else{
        NSLog(@"Accessibility API enabled");
    }
    return TRUE;
}

- (void) dealloc {
    [[NSNotificationCenter defaultCenter] removeObserver:self];
}

@end
