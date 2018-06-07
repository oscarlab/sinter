//
//  SimpleArchiver.h
//

#import <Foundation/Foundation.h>
#import "Sinter.h"

@interface Serializer : NSObject

//  Converts Sinter object to an XML String
+(NSString *) objectToXml:(Sinter *) sinter;

//  Converts an XML String back to a Sinter object
+(Sinter *) xmlToObject:(NSString *) xmlString;


@end
