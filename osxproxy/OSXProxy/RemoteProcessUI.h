//
//  RemoteControl.h
//  Hello
//
//  Created by Syed Masum Billah on 7/12/14.
//
//

#import <Foundation/Foundation.h>

@interface RemoteProcessUI : NSObject

@property (nonatomic, strong) NSString* process_id;
@property (nonatomic, strong) NSString* parent_id;

@property (nonatomic, strong) NSString* unique_id;
@property (nonatomic, strong) NSString* name;
@property (nonatomic, strong) NSString* type;
@property (nonatomic, strong) NSString* value;

//children
@property (nonatomic, strong) NSMutableArray* children;

//optional: to hold custom user data
@property (nonatomic, strong) NSMutableDictionary* user_data;

// atomic properties
@property (assign) unsigned long states; // a bit-mask of states
@property (assign) int version; // 0:new 1:updated
@property (assign) int top, left, width, height, child_count;
@property (assign) int next_sibling, prev_sibling;

// parent
@property (weak) RemoteProcessUI* parent;

- (BOOL) isEqualToUI:(id)object;
- (NSString*) toString; //seialize
- (void) printStates; // show states
@end
