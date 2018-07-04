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
    Config.h
    AppleXMLSerializer

    Created by Syed Masum Billah on 10/16/16.
*/

#import <Foundation/Foundation.h>

@interface Config : NSObject

+ (NSArray *) getProperties:(NSString *) className;
+ (NSDictionary *) getClasses;
+ (NSString *) convertCamelCase2Underscores: (NSString *) input;
+ (NSDictionary *) getServiceCodes;
+ (NSDictionary *) getRoleMappings;
+ (NSDictionary *) getDictFromKey:(NSString *) key;

@end
