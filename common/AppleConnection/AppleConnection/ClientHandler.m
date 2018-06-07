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
    ClientHandler.m
    AppleConnection

    Created by Syed Masum Billah on 10/19/16.
*/

#import "ClientHandler.h"
#import "Serializer.h"
#import "Config.h"


NSString * ClientConnectionDidCloseNotification = @"ClientConnectionDidCloseNotification";

static ClientHandler* shared       = nil;
static NSDictionary * serviceCodes = nil;

@implementation ClientHandler

@synthesize inputStream, outputStream;
@synthesize ipAddress;
@synthesize raw_data, chunk, header, trailer;
@synthesize hasMoreXML, isConnected ;


+(void) initialize {
    serviceCodes = [Config getServiceCodes];
}

+ (id) getConnection {
    return shared;
}

+ (id) sharedConnectionWith:(NSString *)_ipAddress andPort:(int)_port{
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        shared = [[self alloc] init];
        [shared setIpAddress:_ipAddress];
        [shared setPort:_port];
    });
    return shared;
}

- (id) init {
    if (self = [super init]) {
        isConnected = false;

        msgs = [[NSMutableArray alloc] init];
        formatter = [[NSDateFormatter alloc] init];
        [formatter setDateFormat:@"yyyy-MM-dd HH:mm:ss.SSSSSSS"];
        
        
        // xml frame detection
        header = [@"<sinter>" dataUsingEncoding:NSUTF8StringEncoding];
        trailer = [@"</sinter>" dataUsingEncoding:NSUTF8StringEncoding];
        chunk = [[NSMutableData alloc]init];

    }
    return self;
}

- (void) initForClientSocket {
    _isServerSocket = NO;
    
    CFReadStreamRef readStream;
    CFWriteStreamRef writeStream;
    CFStreamCreatePairWithSocketToHost(NULL, (__bridge CFStringRef)ipAddress, _port, &readStream, &writeStream);
    inputStream = (__bridge NSInputStream *)readStream;
    outputStream = (__bridge NSOutputStream *)writeStream;
    
    runLoopMode = NSRunLoopCommonModes;
    
    [self open];

    isConnected = false;
    NSLog(@"connecting to server ...");
}

- (id) initForServerSocketWithtInputStream:(NSInputStream *) inStream outputStream:(NSOutputStream *) outStream andId:(int) identifier {
    if (self = [self init]) {
        // set identifier for notification
        [self setIdentifier:identifier];

        _isServerSocket = YES;
        
        inputStream = inStream;
        outputStream = outStream;
        
        runLoopMode = NSDefaultRunLoopMode;
        [self open];
    }
    return self;
}


- (void) sendMessage: (NSString *) message {
    static NSData *last_data;
    
    NSData *data = [[NSData alloc] initWithData:[message dataUsingEncoding:NSUTF8StringEncoding]];
    if ([last_data isEqualToData:data]) {
        NSLog(@"Same data being sent repeatedly.Skipping send...\n");
        return;
    }
    uint8_t* bytes = (uint8_t*) [message UTF8String];
    NSInteger bytesWritten = 0;
    NSInteger bytesToWrite = [message lengthOfBytesUsingEncoding: NSUTF8StringEncoding];
    
    while ( bytesWritten < bytesToWrite) {
        NSInteger result = [outputStream write:bytes maxLength:(bytesToWrite - bytesWritten)];
        
        if (result <= 0) {
            @throw [NSException exceptionWithName: @"WriterThreadException"
                                           reason: @"Error writing to output stream" userInfo: nil];
        }
        bytesWritten += result;
        bytes        += result;
        //NSLog(@"Bytes to be sent %lu Bytes Actually Sent = %ld\n",  bytesToWrite, bytesWritten);
    }
    
    last_data = data;
    NSLog(@" data sent to server");
}

- (void) sendMessageWithData:(NSData *)data {
    NSString *send_msg = [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
    [self sendMessage:send_msg];
    NSLog(@"Bytes sent %lu\n", (unsigned long)[data length]);
}

- (void) sendSinter:(Sinter *) sinter {
    NSString * send_msg = [Serializer objectToXml:sinter];
    [self sendMessage:send_msg];
}

-(void) dispatchMessage:(NSMutableData *) part {
    // dispatch data to proper listener
    NSString * incoming_data = [[NSString alloc] initWithData:part encoding:NSUTF8StringEncoding];
    
    // deserialize XML
    Sinter * sinter = [Serializer xmlToObject:incoming_data];
    if(!sinter) return;
    
    NSString *notificationName = @"";
    if(_isServerSocket) {
        // for server socket, send message to client_%identifier
        notificationName = [NSString stringWithFormat:@"Client_%i_Notification", _identifier];
        
    } else {
        // service_code for ls_res, and ls_l_res should go to AppDelegate
        // all other messages go to corresponding process_id

        int service_code = [sinter.header.service_code intValue];
        if(service_code == [[serviceCodes objectForKey:@"ls_res"] intValue] ||
           service_code == [[serviceCodes objectForKey:@"ls_l_res"] intValue] ||
           service_code == [[serviceCodes objectForKey:@"window_closed"] intValue]) {
            notificationName = @"AppDelegate";
        } else {
            notificationName = sinter.header.process_id;
        }
    }
    
    [[NSNotificationCenter defaultCenter] postNotificationName:notificationName object:self userInfo:@{@"sinter":sinter}];
}


- (void)stream:(NSStream *) theStream handleEvent:(NSStreamEvent)streamEvent {
    switch (streamEvent) {
        case NSStreamEventOpenCompleted:
            if ([theStream isKindOfClass:[inputStream class]]) {
                NSLog(@"Input stream opened");
            }else{
                NSLog(@"Output stream opened");
            }
            isConnected = true;
            break;
            
        case NSStreamEventHasSpaceAvailable:
            break;
            
        case NSStreamEventHasBytesAvailable:
            if (theStream == inputStream) {
                uint8_t buffer[1024];
                long len = 0;
                if (!(len = [inputStream read:buffer maxLength:sizeof(buffer)]))
                    return ;
                
                chunk = [NSMutableData dataWithBytes:buffer length:len];
                if (!raw_data) { //start of xml is not detected yet
                    NSRange hasPrefix =  [chunk rangeOfData:header options:NSDataSearchAnchored range:NSMakeRange(0, len)];
                    if (!hasPrefix.length) return;
                    raw_data = [[NSMutableData alloc] init];
                }
                
                while (true){
                    NSRange hasSuffix =  [chunk rangeOfData:trailer options:NSDataSearchBackwards range:NSMakeRange(0, len)];
                    if (!hasSuffix.length) {    //wait for next packet
                        [raw_data appendData:chunk];
                        return;
                    }
                    if ((hasSuffix.length + hasSuffix.location) < len) { //does not reached the end of xml
                        [raw_data appendData: [chunk subdataWithRange:NSMakeRange(0, hasSuffix.location+hasSuffix.length)]];
                        hasMoreXML = YES;
                    }
                    if ((hasSuffix.length + hasSuffix.location) == len) { //reached the end of xml
                        [raw_data appendData:chunk];
                        hasMoreXML = NO;
                    }
                    // check if multiple packets are there
                    [msgs removeAllObjects];
                    sublengh = 0 ;
                    while(YES) {
                        pieces =  [raw_data rangeOfData:header options:NSDataSearchBackwards range:NSMakeRange(0, [raw_data length] - sublengh)];
                        NSData * part = [raw_data subdataWithRange:NSMakeRange(pieces.location, [raw_data length] - pieces.location - sublengh)];
                        sublengh += [part length];
                        [msgs insertObject:part atIndex:0];
                        if(!pieces.location && pieces.length){
                            break;
                        }
                    }
                    sublengh = 0;
                    //now dispatch data to proper listener
                    for (NSMutableData* part in msgs) {
                        [self dispatchMessage:part];
                    }
                    [msgs removeAllObjects];
                    //must update data properly
                    if (hasMoreXML) {
                        len = len - (hasSuffix.location + hasSuffix.length);
                        [chunk setData: [chunk subdataWithRange:NSMakeRange(hasSuffix.location + hasSuffix.length, len)]];
                        hasMoreXML = NO;
                        [raw_data setLength:0];
                    }else{
                        raw_data = nil;
                        break;
                    }
                } // end while
            }
            break;
            
        case NSStreamEventErrorOccurred:
        case NSStreamEventEndEncountered:
            [self close];
            isConnected = false;
            break;
            
        default:
            NSLog(@"Unknown event %lu", streamEvent);
    }
}


- (void) open {
    // add delegates
    [inputStream setDelegate:self];
    [outputStream setDelegate:self];
    
    // add run-loops
    [inputStream scheduleInRunLoop:[NSRunLoop currentRunLoop] forMode:runLoopMode];
    [outputStream scheduleInRunLoop:[NSRunLoop currentRunLoop] forMode:runLoopMode]; //NSDefaultRunLoopMode
    
    [inputStream open];
    [outputStream open];
}

- (void) close {
    //[self sendMessage:@"quit"];
    
    [inputStream close];
    [inputStream removeFromRunLoop:[NSRunLoop currentRunLoop] forMode:runLoopMode];
    
    [outputStream close];
    [outputStream removeFromRunLoop:[NSRunLoop currentRunLoop] forMode:runLoopMode];
    
    [inputStream setDelegate:nil];
    inputStream = nil;
    
    [outputStream setDelegate:nil];
    outputStream = nil;
    
    isConnected = false;
    
    if(_isServerSocket) {
        [[NSNotificationCenter defaultCenter] postNotificationName:ClientConnectionDidCloseNotification object:self];
    }
    NSLog(@"disconnected");
}


// utility method for sending message
- (void) sendListRemoteApp {
    Sinter * sinter = [[Sinter alloc] init];
    sinter.header = [[Header alloc] initWithServiceCode:[serviceCodes objectForKey:@"ls_req"]];
    [self sendSinter:sinter];
}

- (void) sendDomRemoteApp:(NSString*) appId {
    Sinter * sinter = [[Sinter alloc] init];
    sinter.header = [[Header alloc] initWithServiceCode:[serviceCodes objectForKey:@"ls_l_req"]];
    sinter.header.process_id = appId;
    [self sendSinter:sinter];
}


- (void) sendActionAt:(NSString *) uniqueId {
    Sinter * sinter = [[Sinter alloc] initWithServiceCode:[serviceCodes objectForKey:@"default_action"] andKbdOrActionWithTarget:uniqueId andData:@""];
    [self sendSinter:sinter];
}

- (void) sendActionAt:(NSString *) uniqueId actionName:(NSString*) action {
    Sinter * sinter = [[Sinter alloc] initWithServiceCode:[serviceCodes objectForKey:@"action"] andKbdOrActionWithTarget:uniqueId andData:action];
    [self sendSinter:sinter];
}

- (void) sendFocusAt:(NSString*) uniqueId {
    Sinter * sinter = [[Sinter alloc] initWithServiceCode:[serviceCodes objectForKey:@"focus"] andKbdOrActionWithTarget:uniqueId andData:@""];
    [self sendSinter:sinter];
}

- (void) sendBtingFG:(NSString*) uniqueId {
    Sinter * sinter = [[Sinter alloc] initWithServiceCode:[serviceCodes objectForKey:@"foreground"] andKbdOrActionWithTarget:uniqueId andData:@""];
    [self sendSinter:sinter];
}


- (void) sendFocusAt:(NSString*) uniqueId andSyncUsingHash:(NSString*) hash {
    Sinter * sinter = [[Sinter alloc] initWithServiceCode:[serviceCodes objectForKey:@"focus"] andKbdOrActionWithTarget:uniqueId andData:hash];
    [self sendSinter:sinter];
}

- (void) setTextAt:(NSString*) uniqueId text:(NSString*) text {
    Sinter * sinter = [[Sinter alloc] initWithServiceCode:[serviceCodes objectForKey:@"settext"] andKbdOrActionWithTarget:uniqueId andData:text];
    [self sendSinter:sinter];
}

- (void) appendTextAt:(NSString*) uniqueId text:(NSString*) text {
    Sinter * sinter = [[Sinter alloc] initWithServiceCode:[serviceCodes objectForKey:@"appendtext"] andKbdOrActionWithTarget:uniqueId andData:text];
    [self sendSinter:sinter];
}

- (void) sendKeystorkesAt:(NSString*) processId strokes:(NSString*) strokes {
    Sinter * sinter = [[Sinter alloc] initWithServiceCode:[serviceCodes objectForKey:@"kbd"] andKbdOrActionWithTarget:processId andData:strokes];
    [self sendSinter:sinter];
}

- (void) sendMouseMoveAt:(NSString*) processId andX:(int) x andY:(int) y {
    Sinter * sinter = [[Sinter alloc] initWithServiceCode:[serviceCodes objectForKey:@"mouse"] andMouseWithX:x andY:y andButton:-1];
    [self sendSinter:sinter];
}

- (void) sendMouseClickAt:(NSString*) processId andX:(int) x andY:(int) y andButton:(int) button {
    Sinter * sinter = [[Sinter alloc] initWithServiceCode:[serviceCodes objectForKey:@"mouse"] andMouseWithX:x andY:y andButton:button];
    [self sendSinter:sinter];
}

- (void) sendCaretMoveAt:(NSString*) runtimeId andLocation:(NSInteger) location andLength:(NSInteger) length {
    Sinter * sinter = [[Sinter alloc] initWithServiceCode:[serviceCodes objectForKey:@"caret"] andCaret:(int)location andLength:(int)length andTarget:runtimeId];
    [self sendSinter:sinter];
}

- (void) sendSpecialStroke:(NSString *) key numRepeat:(int) repeat {
    //Sinter * sinter = [[Sinter alloc] initWithServiceCode:[serviceCodes objectForKey:@"kbd_shortcut"] andKbdOrActionWithTarget:key andData:@(repeat).stringValue];
    Sinter * sinter = [[Sinter alloc] initWithServiceCode:[serviceCodes objectForKey:@"kbd_shortcut"] andKbdOrActionWithTarget:key andData:key];
    [self sendSinter:sinter];
}

@end
