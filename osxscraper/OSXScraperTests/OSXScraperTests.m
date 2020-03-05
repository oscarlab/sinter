/* Copyright (C) 2014--2018 Stony Brook University
   Copyright (C) 2016--2020 The University of North Carolina at Chapel Hill

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
    OSXScrapperTests.m
    OSXScrapperTests

    Created by Syed Masum Billah on 2/26/15.
*/

#import <XCTest/XCTest.h>
#import "AccAPI.h"
#import "Entity.h"
#import "Sinter.h"
#import "Config.h"
#import "XMLTags.h"
#import "Scraper.h"


@interface OSXScrapperTests : XCTestCase

@end

@implementation OSXScrapperTests

NSString * OSXScraperName;
Scraper * scraper;

- (void)setUp
{
    [super setUp];
    
    /* Adding self name to ValidApps and we will scraper self for the test */
    OSXScraperName = [[[NSBundle mainBundle] infoDictionary] objectForKey:(id)kCFBundleNameKey];
    [AccAPI addValidApp:OSXScraperName];
    
    /* create a scraper with no connection */
    scraper = [[Scraper alloc] initWithId:1 andClientHandler:nil] ;
    
}

- (void)tearDown
{
    // Put teardown code here. This method is called after the invocation of each test method in the class.
    [super tearDown];
}

- (void) testLS
{
    //test 'list of applications', should see itself (OSXScraper)
    Sinter * sinterInput = [[Sinter alloc] initWithServiceCode:[serviceCodes objectForKey:STRLsReq]];
    Sinter * sinterOutput = [scraper execute:sinterInput];
    XCTAssertNotNil(sinterOutput);
    XCTAssertTrue(sinterOutput.entities.count >= 1);
    BOOL bFoundSelf = NO;
    for (Entity* e in sinterOutput.entities){
        if ([e.name isEqualToString:OSXScraperName]){
            bFoundSelf = YES;
            NSLog(@"%s %@, %@", __PRETTY_FUNCTION__, e.name, e.process_id);
            break;
        }
    }
    XCTAssertTrue(bFoundSelf);
}

- (void) testLongLS
{
    //first get the process_id of itself, then test long LS (scrape it and output a response sinter
    Sinter * sinterInput = [[Sinter alloc] initWithServiceCode:[serviceCodes objectForKey:STRLsReq]];
    [scraper execute:sinterInput];
    Sinter * sinterOutput = [AccAPI getListOfApplications];
    NSString* process_id;
    for (Entity* e in sinterOutput.entities){
        if ([e.name isEqualToString:OSXScraperName]){
            NSLog(@"%s %@, %@", __PRETTY_FUNCTION__, e.name, e.process_id);
            process_id = e.process_id;
            break;
        }
    }
    Sinter * sinterInput2 = [[Sinter alloc] init];
    sinterInput2.header = [[Header alloc] initWithServiceCode:[serviceCodes objectForKey:STRLsLongReq]
                                                      subCode:[serviceCodes objectForKey:STRLsLongReq]
                                                    processId:process_id
                                                   parameters:nil];
    Sinter * sinterOutput2 = [scraper execute:sinterInput2];
    XCTAssertNotNil(sinterOutput2);
    XCTAssertTrue([sinterOutput2.header.process_id isEqualToString:process_id]);
    XCTAssertTrue([sinterOutput2.header.service_code isEqualToNumber: [serviceCodes objectForKey:STRLsLongRes]]);
    XCTAssertNotNil(sinterOutput2.entity);
}

@end
