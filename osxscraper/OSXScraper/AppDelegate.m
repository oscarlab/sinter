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
    AppDelegate.m
    OSXScrapper

    Created by Syed Masum Billah on 2/26/15.
*/

#import "AppDelegate.h"
#import "ScraperServer.h"
#import "AccAPI.h"

@implementation AppDelegate
@synthesize passcodeTextField;
@synthesize portTextField;
@synthesize startButton;
@synthesize stopButton;
@synthesize server;
@synthesize settings;

- (void)applicationDidFinishLaunching:(NSNotification *)aNotification
{
    settings = [NSDictionary dictionaryWithContentsOfFile:[[NSBundle mainBundle] pathForResource:@"Settings" ofType:@"plist"]];
    
    server = [[ScraperServer alloc] init];
    
    [passcodeTextField setStringValue:@""];
    [portTextField setStringValue:[[settings objectForKey:@"port"] stringValue]];
    stopButton.enabled = NO;
}

- (IBAction)startScraper:(id)sender {
    int port =[[portTextField stringValue] intValue];
    if ( [server start:port] ) {
        NSLog(@"Started server on port %d.", port);
        [portTextField setStringValue:[NSString stringWithFormat:@"%d", port]];
#ifndef DEBUG
        gPasscode = arc4random_uniform(1000000);
#else
        gPasscode = [[settings objectForKey:@"default_passcode"] intValue]; //for testing
#endif
        NSString *passcodeStr = [NSString stringWithFormat:@"%d", gPasscode];
        [passcodeTextField setStringValue:passcodeStr];
        startButton.enabled = NO;
        stopButton.enabled = YES;
    } else {
        NSLog(@"Error starting server");
        NSString *passcodeStr = @"";
        [passcodeTextField setStringValue:passcodeStr];
        
        NSAlert *alert = [[NSAlert alloc] init];
        [alert setMessageText:@"Please check your port setting"];
        [alert runModal];
    }
}

- (IBAction)StopScraper:(id)sender {
    [server stop];
    [passcodeTextField setStringValue:@""];
    startButton.enabled = YES;
    stopButton.enabled = NO;
}

@end
