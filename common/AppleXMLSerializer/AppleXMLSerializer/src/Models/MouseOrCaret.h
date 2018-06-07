//
//  Mouse.h
//  AppleXMLSerializer
//
//  Created by Syed Masum Billah on 10/16/16.
//  Copyright © 2016 Syed Masum Billah. All rights reserved.
//

#import <Foundation/Foundation.h>

@interface MouseOrCaret : NSObject

@property (nonatomic, retain) NSNumber* x_or_starting;
@property (nonatomic, retain) NSNumber* y_or_ending;
@property (nonatomic, retain) NSNumber* button_type;
@property (nonatomic, retain) NSString* target_id;

+ (NSArray *) getSerializableProperties;

- (id) initWithX:(int) x andY:(int) y andButton:(int) button;
- (id) initWithCaret:(int) location andLength:(int) ending andTarget:(NSString*) target_id;

@end
