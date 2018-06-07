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

#import "ScraperServer.h"

//#import "ClientConnection.h"
#import "ClientHandler.h"

#include <sys/socket.h>
#include <netinet/in.h>
#include <unistd.h>
#include "Scraper.h"

static int identifier;



@implementation ScraperServer

@synthesize port;
@synthesize netService;
@synthesize connections;
@synthesize scrapers;
@synthesize ClientConnectionDidCloseNotification;

+ (void) initialize {
    identifier = 0;
}

- (id)init {
    self = [super init];
    if (self != nil) {
        connections = [[NSMutableSet alloc] init];
        scrapers = [[NSMutableDictionary alloc] init];
        
        ClientConnectionDidCloseNotification = @"ClientConnectionDidCloseNotification";
    }
    return self;
}

- (void) dealloc {
    [self stop];
}

- (void) ClientConnectionDidCloseNotification:(NSNotification *)note {
    ClientHandler *connection = [note object];
    
    assert([connection isKindOfClass:[ClientHandler class]]);
    
    [[NSNotificationCenter defaultCenter] removeObserver:self name:ClientConnectionDidCloseNotification object:connection];
    [scrapers removeObjectForKey:[NSNumber numberWithInt:[connection identifier]]];
    [connections removeObject:connection];
    
    NSLog(@"Connection closed.");
}

- (void) acceptConnection:(CFSocketNativeHandle)nativeSocketHandle {
    CFReadStreamRef readStream   = NULL;
    CFWriteStreamRef writeStream = NULL;
    
    CFStreamCreatePairWithSocket(kCFAllocatorDefault, nativeSocketHandle, &readStream, &writeStream);
    if (readStream && writeStream) {
        CFReadStreamSetProperty(readStream, kCFStreamPropertyShouldCloseNativeSocket, kCFBooleanTrue);
        CFWriteStreamSetProperty(writeStream, kCFStreamPropertyShouldCloseNativeSocket, kCFBooleanTrue);

        identifier++;
        ClientHandler * connection = [[ClientHandler alloc]
                                         initForServerSocketWithtInputStream:(__bridge NSInputStream *)readStream
                                         outputStream:(__bridge NSOutputStream *)writeStream
                                         andId:identifier];
        
        Scraper * scraper = [[Scraper alloc] initWithId:identifier andClientHandler:connection] ;
        [scrapers setObject:scraper forKey:[NSNumber numberWithInt: identifier]];
        [connections addObject:connection];
        
        //[connection open];        
        [[NSNotificationCenter defaultCenter]
            addObserver:self
            selector:@selector(ClientConnectionDidCloseNotification:)
            name:ClientConnectionDidCloseNotification
            object:connection];
        
        NSLog(@"Client_%i created", identifier);
    }
    else {
        // On any failure, we need to destroy the CFSocketNativeHandle
        (void) close(nativeSocketHandle);
    }
    
    if (readStream) CFRelease(readStream);
    if (writeStream) CFRelease(writeStream);
}

// This function is called by CFSocket when a new connection comes in.
static void EchoServerAcceptCallBack(CFSocketRef socket, CFSocketCallBackType type, CFDataRef address, const void *data, void *info) {
    assert(type == kCFSocketAcceptCallBack);
    #pragma unused(type)
    #pragma unused(address)
    
    ScraperServer *server = (__bridge ScraperServer *)info;
    assert(socket == server->_ipv4socket);
    #pragma unused(socket)
    
    // For an accept callback, the data parameter is a pointer to a CFSocketNativeHandle.
    [server acceptConnection:*(CFSocketNativeHandle *)data];
}

- (BOOL)start {
    assert(_ipv4socket == NULL);       // don't call -start twice!

    CFSocketContext socketCtxt = {0, (__bridge void *) self, NULL, NULL, NULL};
    _ipv4socket = CFSocketCreate(kCFAllocatorDefault, AF_INET,  SOCK_STREAM, 0, kCFSocketAcceptCallBack, &EchoServerAcceptCallBack, &socketCtxt);

    if (NULL == _ipv4socket) {
        [self stop];
        return NO;
    }

    static const int yes = 1;
    (void) setsockopt(CFSocketGetNative(_ipv4socket), SOL_SOCKET, SO_REUSEADDR, (const void *) &yes, sizeof(yes));

    // Set up the IPv4 listening socket; port is 0, which will cause the kernel to choose a port for us.
    struct sockaddr_in addr4;
    memset(&addr4, 0, sizeof(addr4));
    addr4.sin_len = sizeof(addr4);
    addr4.sin_family = AF_INET;
    addr4.sin_port = htons(6832); //0, 6832; //;
    addr4.sin_addr.s_addr = htonl(INADDR_ANY);
    if (kCFSocketSuccess != CFSocketSetAddress(_ipv4socket, (__bridge CFDataRef) [NSData dataWithBytes:&addr4 length:sizeof(addr4)])) {
        [self stop];
        return NO;
    }
    
    // Now that the IPv4 binding was successful, we get the port number 
    NSData *addr = (__bridge_transfer NSData *)CFSocketCopyAddress(_ipv4socket);
    assert([addr length] == sizeof(struct sockaddr_in));
    self.port = ntohs(((const struct sockaddr_in *)[addr bytes])->sin_port);
    //self.port = 6832;


    // Set up the run loop sources for the sockets.
    CFRunLoopSourceRef source4 = CFSocketCreateRunLoopSource(kCFAllocatorDefault, _ipv4socket, 0);
    CFRunLoopAddSource(CFRunLoopGetCurrent(), source4, kCFRunLoopDefaultMode);
    CFRelease(source4);
    
    return YES;
}

- (void)stop {
    // Closes all the open connections.  The ClientConnectionDidCloseNotification notification will ensure
    for (ClientHandler * connection in [self.connections copy]) {
        [connection close];
    }
    if (_ipv4socket != NULL) {
        CFSocketInvalidate(_ipv4socket);
        CFRelease(_ipv4socket);
        _ipv4socket = NULL;
    }
}

@end
