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
    Entity.h
    testReflection

    Created by Syed Masum Billah on 10/14/16.
*/

#import <Foundation/Foundation.h>

@interface Entity : NSObject ///<>

@property (nonatomic, retain) NSString* unique_id;
@property (nonatomic, retain) NSString* name;
@property (nonatomic, retain) NSString* value;
@property (nonatomic, retain) NSString* type;
@property (nonatomic, retain) NSString* raw_type;
@property (nonatomic, retain) NSString* process_id;
@property (nonatomic, retain) NSNumber* states;
@property (nonatomic, retain) NSNumber* top;
@property (nonatomic, retain) NSNumber* left;
@property (nonatomic, retain) NSNumber* height;
@property (nonatomic, retain) NSNumber* width;
@property (nonatomic, retain) NSNumber* child_count; //deprecated?
@property (nonatomic, retain) NSMutableArray* children;
@property (nonatomic, retain) NSMutableArray* words;

- (id) init;

+ (NSArray *) getSerializableProperties;

@end
