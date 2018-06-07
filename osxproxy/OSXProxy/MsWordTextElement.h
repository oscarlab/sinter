//
//  MsWordTextElement.h
//  NVRDP
//
//  Created by Tabish on 2/26/15.
//  Copyright (c) 2015 Stony Brook University. All rights reserved.
//

#ifndef NVRDP_MsWordTextElement_h
#define NVRDP_MsWordTextElement_h

#import <Foundation/Foundation.h>

@interface MsWordTextElement: NSObject {
}

@property (nonatomic, retain) NSString* text;
@property (nonatomic, retain) NSString* fontName;
@property (nonatomic, retain) NSString* fontSize;

@property (assign) int startOffset, endOffset, pgNo;
@property (assign) BOOL isBold, isItalic, isUnderline;

@end

#endif
