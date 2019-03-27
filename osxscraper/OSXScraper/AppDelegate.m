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

- (void)applicationDidFinishLaunching:(NSNotification *)aNotification
{

    ScraperServer * server = [[ScraperServer alloc] init];
    if ( [server start] ) {
        NSLog(@"Started server on port %zu.", (size_t) [server port]);
        
#ifndef DEBUG
        gPasscode = arc4random_uniform(1000000);
#else
        gPasscode = 123456; //for testing
#endif
        NSString *passcodeStr = [NSString stringWithFormat:@"%d", gPasscode];
        [passcodeTextField setStringValue:passcodeStr];
        
        [[NSRunLoop currentRunLoop] run];
    } else {
        NSLog(@"Error starting server");
        NSString *passcodeStr = @"<Empty>";
        [passcodeTextField setStringValue:passcodeStr];
        
        [[NSRunLoop currentRunLoop] run];
    }

//   // debug
//    ClientConnection * client = [[ClientConnection alloc] init] ;
//    pid_t pid = 262;//224; //
//    NSString * command = [NSString stringWithFormat:@"<sinter><header> <service_code value='3'/>  <timestamp>10-4-2015 10:23 pm </timestamp> </header> <application id='%i'/> </sinter>", pid];
//
//    NSLog(@"%@", command);
//    [client execute:command];
//    

}
@end
