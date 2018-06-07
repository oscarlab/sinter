//
//  Screen.h
//  AppleXMLSerializer
//
//  Created by Syed Masum Billah on 10/16/16.
//  Copyright © 2016 Syed Masum Billah. All rights reserved.
//

#import <Foundation/Foundation.h>

@interface Screen : NSObject

@property (nonatomic, retain) NSNumber* screen_width;
@property (nonatomic, retain) NSNumber* screen_height;

-(id) init;
-(id) initWithHeight:(int) height andWidth:(int) width;

+ (NSArray *) getSerializableProperties;

@end
