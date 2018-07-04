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
    XMLTags.h
    NVRDP

    Created by Syed Masum Billah on 10/19/15.
*/

#ifndef NVRDP_XMLTags_h
#define NVRDP_XMLTags_h

FOUNDATION_EXPORT NSString *const rootTag;
FOUNDATION_EXPORT NSString *const idTag;
FOUNDATION_EXPORT NSString *const headerTag;
FOUNDATION_EXPORT NSString *const serviceCodeTag;
FOUNDATION_EXPORT NSString *const timestampTag;
FOUNDATION_EXPORT NSString *const applicationTag;

FOUNDATION_EXPORT NSString *const processIdTag;
FOUNDATION_EXPORT NSString *const nameTag;
FOUNDATION_EXPORT NSString *const valueTag;
FOUNDATION_EXPORT NSString *const roleTag;
FOUNDATION_EXPORT NSString *const typeTag;
FOUNDATION_EXPORT NSString *const leftTag;
FOUNDATION_EXPORT NSString *const topTag;
FOUNDATION_EXPORT NSString *const widthTag;
FOUNDATION_EXPORT NSString *const heightTag;
FOUNDATION_EXPORT NSString *const statesTag;
FOUNDATION_EXPORT NSString *const childCountTag;


FOUNDATION_EXPORT NSString *const targetIdTag;
FOUNDATION_EXPORT NSString *const updateTypeTag;

FOUNDATION_EXPORT NSString *const eventTag;
FOUNDATION_EXPORT NSString *const eventFGTag;
FOUNDATION_EXPORT NSString *const eventKBDTag;
FOUNDATION_EXPORT NSString *const eventMOUSETag;
FOUNDATION_EXPORT NSString *const eventFocusTag;
FOUNDATION_EXPORT NSString *const eventActionTag;
FOUNDATION_EXPORT NSString *const eventSetTextTag;
FOUNDATION_EXPORT NSString *const eventAppendTextTag;

FOUNDATION_EXPORT NSString *const updateTypeChildUpdated;
FOUNDATION_EXPORT NSString *const updateTypeChildReplaced;
FOUNDATION_EXPORT NSString *const updateTypeNameChanged;
FOUNDATION_EXPORT NSString *const updateTypeValueChanged;
FOUNDATION_EXPORT NSString *const updateTypeChildAdded;
FOUNDATION_EXPORT NSString *const updateTypeFocusChanged;
FOUNDATION_EXPORT NSString *const updateTypeDialog;
FOUNDATION_EXPORT NSString *const updateTypeNodeExpanded;


#endif
