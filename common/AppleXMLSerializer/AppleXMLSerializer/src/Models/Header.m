//
//  Header.m
//  testReflection
//
//  Created by Syed Masum Billah on 10/14/16.
//  Copyright © 2016 Syed Masum Billah. All rights reserved.
//

#import "Header.h"
#import "Config.h"

@implementation Header

static NSArray * props;
static NSDateFormatter * dateFormatter;

+ (void) initialize {
    props = [Config getProperties:NSStringFromClass([self class])];
    
    dateFormatter = [[NSDateFormatter alloc] init];
    [dateFormatter setDateFormat:@"yyyy-MM-dd hh:mm:ss"];
}

+ (NSArray *) getSerializableProperties {
    return props;
}


- (id) init {
    if ( self = [super init] ) {
        _timestamp = [dateFormatter stringFromDate: [NSDate date]];
    }
    return self;
}

- (id) initWithServiceCodeInt:(int) service_code {
    if ( self = [self init] ) {
        _service_code = [NSNumber numberWithInt: service_code];
    }
    return self;
}

- (id) initWithServiceCode:(NSNumber *) service_code {
    if ( self = [self init] ) {
        _service_code = service_code;
    }
    return self;
}

- (id) initWithServiceCode:(NSNumber *) service_code andProcessId:(NSString *) process_id {
    if ( self = [self init] ) {
        _service_code = service_code;
        _process_id = process_id;
    }
    return self;
}


-(void) setService_code:(NSString *)service_code {
    _service_code = [NSNumber numberWithInt: [service_code intValue]];
}

//-(void) setTimestamp:(NSString *) timestamp {
//    _timestamp = [dateFormatter dateFromString:timestamp];
//    
//}

- (NSDate *) getNSDate {
      return [dateFormatter dateFromString:_timestamp];
}


-(void) setProcess_id:(NSString *)process_id {
    _process_id = [NSString stringWithString:process_id];
}

@end
