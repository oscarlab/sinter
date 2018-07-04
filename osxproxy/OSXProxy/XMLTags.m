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
    XMLTags.m
    NVRDP

    Created by Syed Masum Billah on 10/19/15.
*/

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

