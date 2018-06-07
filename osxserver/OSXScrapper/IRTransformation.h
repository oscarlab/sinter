//
//  IRTransformation.h
//  OSXScrapper
//
//  Created by Syed Masum Billah on 10/21/15.
//  Copyright (c) 2015 Stony Brook University. All rights reserved.
//

#import <Foundation/Foundation.h>

@interface IRTransformation : NSObject
+(NSXMLNode *) lookAlike:(NSXMLNode *) app ;
+(NSXMLNode *) performMenuTransfromationOn:(NSXMLNode *) app;
+(NSXMLNode *) performMenuTransfromation2On:(NSXMLNode *) app;
@end
