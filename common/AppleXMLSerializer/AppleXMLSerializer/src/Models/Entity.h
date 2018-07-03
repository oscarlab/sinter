//
//  Entity.h
//  testReflection
//
//  Created by Syed Masum Billah on 10/14/16.
//  Copyright © 2016 Syed Masum Billah. All rights reserved.
//

#import <Foundation/Foundation.h>

@interface Entity : NSObject ///<>

@property (nonatomic, retain) NSString* unique_id;
@property (nonatomic, retain) NSString* name;
@property (nonatomic, retain) NSString* value;
@property (nonatomic, retain) NSString* type;
@property (nonatomic, retain) NSString* process_id;

@property (nonatomic, retain) NSNumber* top;
@property (nonatomic, retain) NSNumber* left;
@property (nonatomic, retain) NSNumber* height;
@property (nonatomic, retain) NSNumber* width;
@property (nonatomic, retain) NSNumber* states;
@property (nonatomic, retain) NSNumber* child_count;

@property (nonatomic, retain) NSMutableArray* children;
@property (nonatomic, retain) NSMutableArray* words;

- (id) init;

+ (NSArray *) getSerializableProperties;

@end
