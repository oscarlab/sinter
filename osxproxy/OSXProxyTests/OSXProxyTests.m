//
//  OSXProxyTests.m
//  OSXProxyTests
//
//  Created by Erica Fu on 2/26/20.
//  Copyright © 2020 UNC Chapel Hill. All rights reserved.
//

#import <XCTest/XCTest.h>
#import "Serializer.h"
#import "Sinter.h"
#import "AppDelegate.h"
#import "Model.h"
#import "CustomWindowController.h"

@interface OSXProxyTests : XCTestCase

@end

@implementation OSXProxyTests

AppDelegate * appDelegate;
- (void)setUp {
    // Put setup code here. This method is called before the invocation of each test method in the class.
    appDelegate = (AppDelegate * )[NSApp delegate];
}

- (void)tearDown {
    // Put teardown code here. This method is called after the invocation of each test method in the class.
}

- (Sinter *)getSinterFromInputFile: (NSString *) filename {
    NSError * error = NULL;
    NSString * incoming_data = [NSString stringWithContentsOfFile:filename encoding:NSUTF8StringEncoding error:&error];
    return [Serializer xmlToObject:incoming_data];
}


- (void)test001_ServiceCode2_empty {
    NSBundle *bundle = [NSBundle bundleForClass:[self class]];
    NSString *inputfile = [bundle pathForResource:@"test001_ServiceCode2_empty" ofType:@"xml"];
    Sinter * sinter = [self getSinterFromInputFile:inputfile];
    XCTAssertNotNil(sinter);
    [appDelegate takeActionForXML:sinter];
    
    // the rendering code is wrapped in dispatch_async() in initWithWindowNibName()
    // run mainRunLoop to let it call the async block
    [[NSRunLoop mainRunLoop] runUntilDate:[NSDate dateWithTimeIntervalSinceNow:0.01]];
    
    // No process is listed, nothing really happens.
    XCTAssertTrue(appDelegate.remoteProcesses == nil || [appDelegate.remoteProcesses count] == 0);
}

- (void)test002_testServiceCode2 {
    NSBundle *bundle = [NSBundle bundleForClass:[self class]];
    NSString *inputfile = [bundle pathForResource:@"test002_testServiceCode2" ofType:@"xml"];
    Sinter * sinter = [self getSinterFromInputFile:inputfile];
    XCTAssertNotNil(sinter);
    NSUInteger n_processes = [sinter.entities count];
    [appDelegate takeActionForXML:sinter];
    
    // the rendering code is wrapped in dispatch_async() in initWithWindowNibName()
    // run mainRunLoop to let it call the async block
    [[NSRunLoop mainRunLoop] runUntilDate:[NSDate dateWithTimeIntervalSinceNow:0.01]];
    
    XCTAssertTrue([appDelegate.remoteProcesses count] == n_processes);
    NSLog(@"name = %@", ((Model*)(appDelegate.remoteProcesses[0])).name); //process_id, unique_id
}

- (void)test003_ServiceCode4 {
    NSBundle *bundle = [NSBundle bundleForClass:[self class]];
    NSString *inputfile = [bundle pathForResource:@"test003_ServiceCode4" ofType:@"xml"];
    Sinter * sinter = [self getSinterFromInputFile:inputfile];
    XCTAssertNotNil(sinter);
    [appDelegate takeActionForXML:sinter];
    
    // the rendering code is wrapped in dispatch_async() in initWithWindowNibName()
    // run mainRunLoop to let it call the async block
    [[NSRunLoop mainRunLoop] runUntilDate:[NSDate dateWithTimeIntervalSinceNow:0.01]];
    
    NSMutableArray* renderedWindows = [appDelegate.remoteWindowControllers objectForKey:sinter.header.process_id];
    XCTAssertTrue([renderedWindows count] >= 1);
    CustomWindowController * renderWindow = renderedWindows[0];
    XCTAssertTrue([sinter.entity.unique_id isEqualToString:renderWindow.rmUiRoot.unique_id]);
    NSView* view = [renderWindow.window contentView];
    XCTAssertNotNil(view);
    
    // more to do:
    // check MENU:      CustomWindowController->remoteMenu
    // check App DOM:   [renderWindow.window contentView]
}

@end
