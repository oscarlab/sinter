//
//  Action.h
//  AppleXMLSerializer
//
//  Created by Syed Masum Billah on 10/16/16.
//  Copyright © 2016 Syed Masum Billah. All rights reserved.
//

#import <Foundation/Foundation.h>

@interface KbdOrAction : NSObject

@property (nonatomic, retain) NSString* target_id;
@property (nonatomic, retain) NSString* generic_data;

- (id) initWithTarget:(NSString*) target_id andData:(NSString*) data;

+ (NSArray *) getSerializableProperties;

@end
