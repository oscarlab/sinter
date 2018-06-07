//
//  SharedQueue.h
//  NVRDP
//
//  Created by Syed Masum Billah on 10/20/15.
//  Copyright (c) 2015 Stony Brook University. All rights reserved.
//

#ifndef NVRDP_SharedQueue_h
#define NVRDP_SharedQueue_h

#include "Sinter.h"

@protocol SharedQueueDelegate <NSObject>

//- (void) takeActionForXML:(NSXMLDocument*) xmlDoc withServiceCode:(NSString *) service_code;
- (void) takeActionForXML:(Sinter*) xmlDoc;

@end

#endif
