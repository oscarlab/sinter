//
//  Sinter.h
//  testReflection
//
//  Created by Syed Masum Billah on 10/14/16.
//  Copyright © 2016 Syed Masum Billah. All rights reserved.
//

#import <Foundation/Foundation.h>
#include "Header.h"
#include "Entity.h"


@interface Sinter : NSObject
@property (nonatomic, retain) Header*         header;
@property (nonatomic, retain) Entity*         entity;
@property (nonatomic, retain) NSMutableArray* updates;
@property (nonatomic, retain) NSMutableArray* applications;

- (id) init;
- (id) initWithEntity;
- (id) initWithApplications;
- (id) initWithUpdates;

+ (NSArray *) getSerializableProperties;

@end




