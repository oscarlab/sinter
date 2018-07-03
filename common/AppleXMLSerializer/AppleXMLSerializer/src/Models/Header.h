//
//  Header.h
//  testReflection
//
//  Created by Syed Masum Billah on 10/14/16.
//  Copyright © 2016 Syed Masum Billah. All rights reserved.
//

#import <Foundation/Foundation.h>

#import "Screen.h"
#import "MouseOrCaret.h"
#import "KbdOrAction.h"
#import "Header.h"

@interface Header : NSObject 

@property (nonatomic, assign) NSNumber* service_code;
@property (nonatomic, retain) NSString* timestamp;
@property (nonatomic, retain) NSString* process_id;

@property (nonatomic, retain) Screen*       screen;
@property (nonatomic, retain) KbdOrAction*  kbd_or_action;
@property (nonatomic, retain) MouseOrCaret* mouse_or_caret;


- (id) init;
- (id) initWithServiceCodeInt:(int) service_code;
- (id) initWithServiceCode:(NSNumber *) service_code;
- (id) initWithServiceCode:(NSNumber *) service_code andProcessId:(NSString *) process_id;

- (NSDate *) getNSDate;

+ (NSArray *) getSerializableProperties;

@end
