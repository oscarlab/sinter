//
//  Entity.h
//  testReflection
//
//  Created by Syed Masum Billah on 10/14/16.
//  Copyright � 2016 Syed Masum Billah. All rights reserved.
//

#import <Foundation/Foundation.h>

@interface Word : NSObject

@property (nonatomic, retain) NSString* text;
@property (nonatomic, retain) NSString* font_name;
@property (nonatomic, retain) NSString* font_size;
@property (nonatomic, retain) NSString* bold;
@property (nonatomic, retain) NSString* italic;
@property (nonatomic, retain) NSString* underline;
@property (nonatomic, retain) NSString* newline;

- (id) init;

+ (NSArray *) getSerializableProperties;

@end
