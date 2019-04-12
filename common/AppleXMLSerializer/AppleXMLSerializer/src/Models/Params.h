//
//  Params.h
//  OSXProxy
//
//  Created by Erica Fu on 2/28/19.
//  Copyright © 2019 Stony Brook University. All rights reserved.
//

#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

@interface Params : NSObject

@property (nonatomic, retain) NSString* target_id;
@property (nonatomic, retain) NSMutableArray* target_id_list;
@property (nonatomic, retain) NSString* data1;
@property (nonatomic, retain) NSString* data2;
@property (nonatomic, retain) NSString* data3;
@property (nonatomic, retain) NSNumber* keypress;

- (id) init;

+ (NSArray *) getSerializableProperties;

@end

NS_ASSUME_NONNULL_END
