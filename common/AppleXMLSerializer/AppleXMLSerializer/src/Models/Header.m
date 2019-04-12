/* Copyright (C) 2014--2018 Stony Brook University
   Copyright (C) 2016--2018 The University of North Carolina at Chapel Hill

   This file is part of the Sinter Remote Desktop System.

   Sinter is dual-licensed, available under a commercial license or
   for free subject to the LGPL.  

   Sinter is free software: you can redistribute it and/or modify it
   under the terms of the GNU Lesser General Public License as published by
   the Free Software Foundation, either version 3 of the License, or
   (at your option) any later version.  Sinter is distributed in the
   hope that it will be useful, but WITHOUT ANY WARRANTY; without even
   the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
   PURPOSE.  See the GNU Lesser General Public License for more details.  You
   should have received a copy of the GNU Lesser General Public License along
   with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

/*
    Header.m
    testReflection

    Created by Syed Masum Billah on 10/14/16.
*/

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

/*
- (id) initWithServiceCodeInt:(int) service_code {
    if ( self = [self init] ) {
        _service_code = [NSNumber numberWithInt: service_code];
        _sub_code = [NSNumber numberWithInt: service_code];
    }
    return self;
}
 */

- (id) initWithServiceCode:(NSNumber *) service_code {
    NSLog(@"service_code is same as sub_code = %@ ?", service_code);
    if ( self = [self init] ) {
        _service_code = service_code;
        _sub_code = service_code;
    }
    return self;
}


- (id) initWithServiceCode:(NSNumber *)service_code subCode:(NSNumber *)sub_code processId:(NSString*)process_id parameters:(Params*)params
{
    if ( self = [self init] ) {
        _service_code = service_code;
        _sub_code = sub_code;
        _process_id = process_id;
        _params = params;
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
