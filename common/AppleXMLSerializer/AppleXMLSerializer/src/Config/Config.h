//
//  Config.h
//  AppleXMLSerializer
//
//  Created by Syed Masum Billah on 10/16/16.
//  Copyright © 2016 Syed Masum Billah. All rights reserved.
//

#import <Foundation/Foundation.h>

@interface Config : NSObject

+ (NSArray *) getProperties:(NSString *) className;
+ (NSDictionary *) getClasses;
+ (NSString *) convertCamelCase2Underscores: (NSString *) input;
+ (NSDictionary *) getServiceCodes;
+ (NSDictionary *) getRoleMappings;
+ (NSDictionary *) getDictFromKey:(NSString *) key;

@end
