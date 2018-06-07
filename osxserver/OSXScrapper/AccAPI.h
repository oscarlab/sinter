//
//  AccAPI.h
//  OSXScrapper
//
//  Created by Syed Masum Billah on 10/9/15.
//  Copyright (c) 2015 Stony Brook University. All rights reserved.
//

#import <Foundation/Foundation.h>
#include "Sinter.h"

@interface AccAPI : NSObject

+ (Sinter *) getListOfApplications;

+ (Sinter *) getDomOf:(pid_t) pid
             andReturnRef:(AXUIElementRef*) elemRef
             withCache:(NSMutableDictionary*) cache;

+ (Sinter *) getDeltaAt:(AXUIElementRef) element
             havingPID:(pid_t) pid
             andUpdateType:(NSString *) updateType
             withCache:(NSMutableDictionary*) cache ;


+ (NSDictionary *) getSegmentedIdOfUIElement: (AXUIElementRef) element
                   andReturnKeys:(NSMutableArray**) key_list;

+ (Entity *) getEntityForApp:(pid_t) pid;

+ (Entity *) getEntityFroElement:(AXUIElementRef)
             element atIndex:(int) index
             havingId:(NSString*) elemId
             andParentId:(NSString *) parentId
             withCache:(NSMutableDictionary*) cache
             updateCache:(bool) whenAsked;

+ (void) printObserverStatus:(AXError) code;

@end
