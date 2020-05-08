//
//  OSXProxyUITests.m
//  OSXProxyUITests
//
//  Created by Erica Fu on 4/16/20.
//  Copyright © 2020 Stony Brook University. All rights reserved.
//

#import <XCTest/XCTest.h>

/*
#import <libxml/tree.h>
#import <libxml/parser.h>
#import <libxml/xmlstring.h>
#import <libxml/xpath.h>
#import <libxml/xpathInternals.h>
#import "Serializer.h"
#import "Sinter.h"
#import "AppDelegate.h"
#import "Model.h"
 */


@interface OSXProxyUITests : XCTestCase

@end

@implementation OSXProxyUITests

- (void)setUp {
    // Put setup code here. This method is called before the invocation of each test method in the class.

    // In UI tests it is usually best to stop immediately when a failure occurs.
    self.continueAfterFailure = NO;

    // In UI tests it’s important to set the initial state - such as interface orientation - required for your tests before they run. The setUp method is a good place to do this.
}

- (void)tearDown {
    // Put teardown code here. This method is called after the invocation of each test method in the class.
}

/*
- (void)testLaunchPerformance {
    if (@available(macOS 10.15, iOS 13.0, tvOS 13.0, *)) {
        // This measures how long it takes to launch your application.
        [self measureWithMetrics:@[XCTOSSignpostMetric.applicationLaunchMetric] block:^{
            [[[XCUIApplication alloc] init] launch];
        }];
    }
}
 */


- (void)testMainUI {
    // UI tests must launch the application that they test.
    XCUIApplication *app = [[XCUIApplication alloc] init];
    app.launchEnvironment = @{@"isUITest": @YES};
    [app launch];

    // Use recording to get started writing UI tests.
    XCUIElement *remoteDesktopWindow = [[XCUIApplication alloc] init].windows[@"Remote Desktop"];
    XCUIElement *connectButton = remoteDesktopWindow.buttons[@"Connect"];
    XCUIElement *disconnectButton = remoteDesktopWindow.buttons[@"Disconnect"];

    [connectButton click];
    XCTAssertTrue(connectButton.enabled == false);
    XCTAssertTrue(disconnectButton.enabled == true);
    
    [disconnectButton click];
    XCTAssertTrue(connectButton.enabled == true);
    XCTAssertTrue(disconnectButton.enabled == false);
}

@end
