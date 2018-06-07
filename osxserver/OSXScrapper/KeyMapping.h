//
//  KeyMapping.h
//  OSXScrapper
//
//  Created by Pavan Manjunath on 3/1/15.
//  Copyright (c) 2015 Stony Brook University. All rights reserved.
//

#import <Foundation/Foundation.h>

@interface KeyMapping : NSObject

+ (NSString *) keyStringFormKeyCode:(CGKeyCode)keyCode;
+ (CGKeyCode)keyCodeFormKeyString:(NSString *)keyString;

@end
