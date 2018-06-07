//
//  Scraper.h
//  OSXScrapper
//
//  Created by Syed Masum Billah on 10/19/16.
//  Copyright © 2016 Stony Brook University. All rights reserved.
//

#import <Foundation/Foundation.h>
#include "ClientHandler.h"

@interface Scraper : NSObject

@property (nonatomic , retain) ClientHandler * clientHandler;
@property (assign) int identifier;

@property (nonatomic, strong ) NSMutableDictionary* appCache;


- (id) initWithId:(int) identifier andClientHandler:(ClientHandler *) clientHandler;


@end
