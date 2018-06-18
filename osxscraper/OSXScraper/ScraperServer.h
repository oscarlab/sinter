
#import <Foundation/Foundation.h>

@interface ScraperServer : NSObject<NSStreamDelegate>{
    CFSocketRef             _ipv4socket;
    CFSocketRef             _ipv6socket;
    
}

@property (nonatomic, assign, readwrite) NSUInteger         port;
@property (nonatomic, strong, readwrite) NSNetService *     netService;
@property (nonatomic, strong, readonly ) NSMutableSet *     connections;
@property (nonatomic, strong, readonly ) NSMutableDictionary *  scrapers;

@property (nonatomic, strong) NSString * ClientConnectionDidCloseNotification;

- (BOOL) start;
- (void) stop;

@end

