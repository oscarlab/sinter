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
    AccAPI.h
    OSXScrapper

    Created by Syed Masum Billah on 10/9/15.
*/

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
