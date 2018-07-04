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
    Config.m
    AppleXMLSerializer

    Created by Syed Masum Billah on 10/16/16.
*/

#import "Config.h"

@implementation Config

static NSDictionary * config;

+ (void) initialize {
    NSError *errorDesc;
    NSPropertyListFormat format;
    
    NSString *plistPath = [[NSBundle mainBundle] pathForResource:@"RoleMapping" ofType:@"plist"];
    NSData *plistXML = [[NSFileManager defaultManager] contentsAtPath:plistPath];
    
    config = (NSDictionary *)[NSPropertyListSerialization
                                            propertyListWithData:plistXML
                                            options:NSPropertyListImmutable
                                            format:&format error: &errorDesc];
    if (!config) {
        NSLog(@"Error reading plist: %@, format: %lu", errorDesc, (unsigned long)format);
    }
}

+ (NSArray *) getProperties:(NSString *) className {
    NSDictionary * dict = [config objectForKey:@"entity_classes"];
    if (dict) {
        return [dict objectForKey:className];
    }
    return nil;
}


+ (NSString *) convertCamelCase2Underscores: (NSString *) input {
    NSMutableString *output = [NSMutableString string];
    NSCharacterSet *uppercase = [NSCharacterSet uppercaseLetterCharacterSet];
    
    unichar c;
    for (NSInteger idx = 0; idx < [input length]; idx += 1) {
        c = [input characterAtIndex:idx];
        if(!idx) {
            [output appendFormat:@"%@", [[NSString stringWithCharacters:&c length:1] lowercaseString]];
        }
        else if ([uppercase characterIsMember:c]) {
            [output appendFormat:@"_%@", [[NSString stringWithCharacters:&c length:1] lowercaseString]];
        }
        else {
            [output appendFormat:@"%C", c];
        }
    }
    return output;
}

+ (NSDictionary *) getClasses {
    NSDictionary * dict = [config objectForKey:@"entity_classes"];
    if (!dict) return nil;
    
    NSMutableDictionary* new_dict = [[NSMutableDictionary alloc] init];
    // generate variation of class-names
    for (NSString * class in [dict allKeys]) {        
        [new_dict setObject:class forKey:class];
        [new_dict setObject:class forKey:[class lowercaseString]];
        [new_dict setObject:class forKey:[self convertCamelCase2Underscores:class]];
    }
    return [new_dict copy];
}


+ (NSDictionary *) getServiceCodes {
    NSDictionary * dict = [config objectForKey:@"service_code"];
    return dict;
}

+ (NSDictionary *) getRoleMappings {
    NSDictionary * dict = [config objectForKey:@"role_mappings"];
    return dict;
}

+ (NSDictionary *) getDictFromKey:(NSString *) key {
    return [config objectForKey:key];
}

@end
