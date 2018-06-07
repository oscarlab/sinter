//
//  KeyMapping.h
//  Hello
//
//  Created by Syed Masum Billah on 7/10/14.
//
//

#import <Foundation/Foundation.h>

@interface KeyMapping : NSObject

+ (NSString *) keyStringFormKeyCodeSimplified:(CGKeyCode)keyCode;
+ (NSString *) keyStringFormKeyCode:(CGKeyCode)keyCode;
+ (CGKeyCode)keyCodeFormKeyString:(NSString *)keyString;

@end
