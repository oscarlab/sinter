//
//  AppDelegate.m
//  OSXScrapper
//
//  Created by Syed Masum Billah on 2/26/15.
//  Copyright (c) 2015 Stony Brook University. All rights reserved.
//

#import "AppDelegate.h"
#import "ScraperServer.h"
#import "AccAPI.h"



@implementation AppDelegate

- (void)applicationDidFinishLaunching:(NSNotification *)aNotification
{

    ScraperServer * server = [[ScraperServer alloc] init];
    if ( [server start] ) {
        NSLog(@"Started server on port %zu.", (size_t) [server port]);
        [[NSRunLoop currentRunLoop] run];
    } else {
        NSLog(@"Error starting server");
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
