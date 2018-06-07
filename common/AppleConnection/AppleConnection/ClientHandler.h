//
//  ClientHandler.h
//  AppleConnection
//
//  Created by Syed Masum Billah on 10/19/16.
//  Copyright © 2016 Syed Masum Billah. All rights reserved.
//

#import <Foundation/Foundation.h>
#import <CFNetwork/CFSocketStream.h>

#import "Sinter.h"

@interface ClientHandler : NSObject<NSStreamDelegate> {
    NSInputStream *inputStream;
    NSOutputStream *outputStream;
    NSMutableArray* msgs;
    NSRange pieces;
    long sublengh;
    NSDateFormatter* formatter;
    NSRunLoopMode runLoopMode;
}

// to discriminate between server vs. client client
@property bool isServerSocket;
@property (assign) int identifier;

// stream related datastructures
@property (nonatomic, retain) NSInputStream *inputStream;
@property (nonatomic, retain) NSOutputStream *outputStream;

// ip-address & port
@property (nonatomic, retain) NSString *ipAddress;
@property int port;
@property bool isConnected;



// xml frame detection
@property (nonatomic, retain) NSData *header, *trailer;
@property bool hasMoreXML; //is reached to the end of XML
@property (nonatomic, retain) NSMutableData* raw_data;
@property (nonatomic, retain) NSMutableData* chunk;


// singleton reference of connection
+ (id) getConnection ;
+ (id) sharedConnectionWith:(NSString *)_ipAddress andPort:(int)_port;
- (void) initForClientSocket;
- (id) initForServerSocketWithtInputStream:(NSInputStream *) inStream outputStream:(NSOutputStream *) outStream andId:(int) identifier ;
- (void) dispatchMessage:(NSMutableData *) part;


// primary send-receive methods
- (void) sendSinter : (Sinter *)  sinter;
- (void) sendMessage: (NSString *) message;
- (void) sendMessageWithData: (NSData *) data;

- (void) close;

@end
