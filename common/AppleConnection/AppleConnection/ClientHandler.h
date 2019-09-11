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
    ClientHandler.h
    AppleConnection
  
    Created by Syed Masum Billah on 10/19/16.
*/

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
    NSString *runLoopMode;
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
@property bool isSelfSignedCertAccepted;


// xml frame detection
@property (nonatomic, retain) NSData *header, *trailer;
@property bool hasMoreXML; //is reached to the end of XML
@property (nonatomic, retain) NSMutableData* raw_data;
@property (nonatomic, retain) NSMutableData* chunk;


// singleton reference of connection
+ (id) getConnection ;
+ (id) sharedConnectionWith:(NSString *)_ipAddress andPort:(int)_port;
- (void) setIPAndPort:(NSString *)_ipAddress andPort:(int)_port;
- (void) initForClientSocket;
- (id) initForServerSocketWithtInputStream:(NSInputStream *) inStream outputStream:(NSOutputStream *) outStream andId:(int) identifier certificatePath:(NSString *)certificatePath certPasscode: (NSString*)certPass;
- (void) dispatchMessage:(NSMutableData *) part;
- (void) close;

// primary send-receive methods
- (void) sendSinter : (Sinter *)  sinter;
- (void) sendMessage: (NSString *) message;
- (void) sendMessageWithData: (NSData *) data;


// utility method for sending message
- (void) sendPasscodeVerifyReq:(NSString *) passcode;
- (void) sendPasscodeVerifyRes:(bool) result;
- (void) sendListRemoteApp;
- (void) sendDomRemoteApp:(NSString*) appId;
/* sendActionMsg to replace old sendActionAt, sendFocusAt, appendTextAt */
- (void) sendActionMsg:(NSString *)processId targetId:(NSString*)targetId actionType:(NSString*)action data:(NSString*)data;
- (void) sendBtingFG:(NSString*) uniqueId;
- (void) setTextAt:(NSString*) uniqueId text:(NSString*) text;

- (void) sendKeystorkesAt:(NSString*) processId strokes:(NSString*) strokes;
/*
- (void) sendMouseMoveAt:(NSString*) processId andX:(int) x andY:(int) y;
- (void) sendMouseClickAt:(NSString*) processId andX:(int) x andY:(int) y andButton:(int) button;
 */
- (void) sendCaretMoveAt:(NSString*) runtimeId andLocation:(NSInteger) location andLength:(NSInteger) length;

- (void) sendSpecialStroke:(NSString *) key numRepeat:(int) repeat;
- (void) sendKeystrokes:(NSString *)key processId:(NSString*)processId targetId:(NSString*)targetId;
- (void) sendEventClosed:(NSString*) process_id;


@end
