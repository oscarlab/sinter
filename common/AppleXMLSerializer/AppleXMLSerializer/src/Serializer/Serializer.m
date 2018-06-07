//
//  Serializer.m
//  testReflection
//
//  Created by Syed Masum Billah on 10/14/16.
//  Copyright © 2016 Syed Masum Billah. All rights reserved.
//

#import "Serializer.h"
#import "GDataXMLNode.h"
#import "Config.h"

static NSDictionary * classLookup;

@implementation Serializer

+ (void) initialize {
    classLookup = [Config getClasses];
}

//  Encodes an object to an XML String
+(NSString *) objectToXml:(Sinter *) sinter {
    NSString* objectNameStr = [NSStringFromClass([sinter class]) lowercaseString];
    GDataXMLElement *rootElement = [GDataXMLElement elementWithName:objectNameStr];
    [self serializeObj:sinter havingParent:rootElement];
    
    // Create the resulting XML document and return it
    GDataXMLDocument *doc = [[GDataXMLDocument alloc] initWithRootElement:rootElement];
    return [[doc rootElement] XMLString ];
    //NSString *result = [[NSString alloc] initWithData:[doc XMLData] encoding:NSUTF8StringEncoding];
    //return result;
}


+(void) serializeObj:(id) obj havingParent:(GDataXMLElement *) parent {
    
    NSArray* props = [[obj class] getSerializableProperties];
    
    for (NSString* prop in props) {
        id value = [obj valueForKey:prop];
        if (!value) continue;
        
        if ([value isKindOfClass:[NSString class]]) {
            [parent addAttribute: [GDataXMLNode attributeWithName:prop stringValue:value]];
        }
        else if ([value isKindOfClass:[NSNumber class]]) {
            [parent addAttribute:[GDataXMLNode attributeWithName:prop stringValue:[value stringValue]]];
        }
        else if ([value isKindOfClass:[NSMutableArray class]]) { // children, applications, updates
            GDataXMLElement* current = [GDataXMLElement elementWithName:prop];
            for ( id elem in value) {
                GDataXMLElement *child = [GDataXMLElement elementWithName:
                                          [NSStringFromClass([elem class]) lowercaseString]];
                [self serializeObj:elem havingParent:child];
                [current addChild:child];
            }
            [parent addChild: current];
        }
        else { // header, entity
            GDataXMLElement* current = [GDataXMLElement elementWithName:prop];
            [self serializeObj:value havingParent:current];
            [parent addChild: current];
        }
    }
}


+(Sinter *) xmlToObject:(NSString *) xmlString {
    NSError *error = [NSError errorWithDomain:@"Serialization" code:(-1) userInfo:nil];
    GDataXMLDocument *doc = [[GDataXMLDocument alloc] initWithXMLString:xmlString options:0 error:&error];
    if (!doc) return nil;
    
    GDataXMLElement *rootElement = [doc rootElement];
    NSString* rootName = [classLookup objectForKey:[rootElement localName]];
    
    
    if(!rootName) {
        return nil;
    }
    
    id obj = [self deSerialize:rootElement targetClass:NSClassFromString(rootName)];
    return obj;
    
}

+(Sinter *) xmlToObject2:(NSString *) xmlString {
    
    NSError *error = [NSError errorWithDomain:@"Serialization" code:(-1) userInfo:nil];
    GDataXMLDocument *doc = [[GDataXMLDocument alloc] initWithXMLString:xmlString options:0 error:&error];
    if (!doc) return nil;
    
    GDataXMLElement *rootElement = [doc rootElement];
    NSString* rootName =  [[rootElement localName] capitalizedString];
    
    if(![rootName isEqualToString:
         NSStringFromClass([Sinter class])]) {
        return nil;
    }
    
    Sinter * obj = [[Sinter alloc] init];
    
    NSString * prop;
    for (GDataXMLElement* elem in [rootElement children]) {
        id elem_obj;
        
        prop = [elem localName];
        if ([prop isEqualToString:@"header"]){
            elem_obj = [self deSerializeHeader: elem];
        }
        else if ([prop isEqualToString:@"entity"]) {
            elem_obj = [self deSerializeEntity: elem];
        }
        else if ([prop isEqualToString:@"updates"]) {
            elem_obj = [self deSerializeUpdates: elem];
        }
        else if ([prop isEqualToString:@"applications"]) {
            elem_obj = [self deSerializeApplications: elem];
        }
        else {
            NSLog(@"Unknown properties");
        }
        
        if (elem_obj) {
            [obj setValue:elem_obj forKey:prop];
        }
        
//        SEL action = NSSelectorFromString(@"someMethod:elem:");
//        NSMethodSignature *signature = [myObject methodSignatureForSelector:action];
//        NSInvocation *invocation = [NSInvocation invocationWithMethodSignature:signature];
//        [invocation setArgument:arg1 atIndex:2]; // indices 0 and 1 are reserved.
//        [invocation setArgument:arg2 atIndex:3];
//        [invocation invokeWithTarget:myObject];
//        id returnedObject;
//        [invocation1 getReturnValue:&returnedObject];
        
    }
    return obj;
}

+(id) deSerialize:(GDataXMLElement *) elem targetClass:(Class) class {
    id obj = [[class alloc] init];
    for (GDataXMLNode * attr in [elem attributes]) {
        [obj setValue:[attr stringValue] forKey:[attr localName]];
    }
    
    NSArray * children = [elem children];
    if (!children) {
        return obj;
    }
    
    for (GDataXMLElement * child in children) {
        NSString *prop_name = [child localName];
        NSString *class_name = [classLookup objectForKey:prop_name];
        
        if (class_name){
            id sub_obj = [self deSerialize:child targetClass:NSClassFromString(class_name)];
            if (sub_obj) {
                [obj setValue:sub_obj forKey:prop_name];
            }
        }
        else { // not a class-name, so it's a dummy array container
            NSMutableArray * babies = [[NSMutableArray alloc] init];
            
            NSString* baby_class;
            id baby_obj;
            for (GDataXMLElement * baby in [child children]) {
                baby_class = [classLookup objectForKey:[baby localName]];
                if (!baby_class) {
                    continue;
                }
                baby_obj = [self deSerialize:baby targetClass:NSClassFromString(baby_class)];
                [babies addObject:baby_obj];
            }
            
            [obj setValue:babies forKey:prop_name];
            //[entity setChild_count:[NSNumber numberWithInt:i]];
        }
    }
    return obj;
}

+(id) deSerializeHeader:(GDataXMLElement *) elem {
    Header * header = [[Header alloc] init];
    for (GDataXMLNode * attr in [elem attributes]) {
        [header setValue:[attr stringValue] forKey:[attr localName]];
    }
    
    NSArray * opt_header = [elem children];
    if (!opt_header) {
        return header;
    }
//    for (GDataXMLElement * child in [opt_header children]) {
//        [entity.children addObject:[self deSerializeEntity:child]];
//        i++;
//    }
    return header;
}

+(id) deSerializeEntity:(GDataXMLElement *) elem {
    Entity * entity = [[Entity alloc] init];
    for (GDataXMLNode * attr in [elem attributes]) {
        [entity setValue:[attr stringValue] forKey:[attr localName]];
    }
    
    NSArray * children = [elem children];
    if (!children) {
        return entity;
    }
    entity.children = [[NSMutableArray alloc] init];
    int i = 0 ;
    for (GDataXMLElement * child in [[children firstObject] children]) {
        [entity.children addObject:[self deSerializeEntity:child]];
        i++;
    }
    [entity setChild_count:[NSNumber numberWithInt:i]];
    
    return entity;
}

+(id) deSerializeApplications:(GDataXMLElement *) elem {
    NSMutableArray *applications = [[NSMutableArray alloc] init];
    for (GDataXMLElement * child in [elem children]) {
        [applications addObject:[self deSerializeEntity:child]];
    }
    return applications;
}

+(id) deSerializeUpdates:(GDataXMLElement *) elem {
    NSMutableArray *updates = [[NSMutableArray alloc] init];
    for (GDataXMLElement * child in [elem children]) {
        [updates addObject:[self deSerializeEntity:child]];
    }
    return updates;
}

@end
