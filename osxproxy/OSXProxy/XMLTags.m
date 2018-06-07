//
//  XMLTags.m
//  NVRDP
//
//  Created by Syed Masum Billah on 10/19/15.
//  Copyright (c) 2015 Stony Brook University. All rights reserved.
//

#import <Foundation/Foundation.h>


NSString *const rootTag              = @"sinter";
NSString *const idTag                = @"id";
NSString *const headerTag            = @"header";
NSString *const timestampTag         = @"timestamp";
NSString *const serviceCodeTag       = @"service_code";
NSString *const applicationTag       = @"application";
NSString *const processIdTag         = @"process_id";
NSString *const nameTag              = @"name";
NSString *const valueTag             = @"value";
NSString *const roleTag              = @"role";
NSString *const typeTag              = @"type";
NSString *const leftTag              = @"left";
NSString *const topTag               = @"top";
NSString *const widthTag             = @"width";
NSString *const heightTag            = @"height";
NSString *const statesTag            = @"states";
NSString *const childCountTag        = @"child_count";
NSString *const targetIdTag          = @"target_id";
NSString *const updateTypeTag        = @"update_type";


NSString *const eventTag             = @"event";
NSString *const eventFGTag           = @"fg";
NSString *const eventKBDTag          = @"kbd";
NSString *const eventMOUSETag        = @"mouse";
NSString *const eventFocusTag        = @"focus";
NSString *const eventActionTag       = @"action";
NSString *const eventSetTextTag      = @"settext";
NSString *const eventAppendTextTag   = @"appendtext";

#pragma mark update type
NSString *const updateTypeChildUpdated     = @"child_updated";
NSString *const updateTypeChildReplaced    = @"child_replaced";
NSString *const updateTypeNameChanged      = @"name_changed";
NSString *const updateTypeValueChanged     = @"value_changed";
NSString *const updateTypeChildAdded       = @"child_added";
NSString *const updateTypeFocusChanged     = @"focus_changed";
NSString *const updateTypeNodeExpanded     = @"node_expanded";
NSString *const updateTypeDialog           = @"dialog";

