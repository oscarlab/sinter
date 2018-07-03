//
//  AccAPI.m
//  OSXScrapper
//
//  Created by Syed Masum Billah on 10/9/15.
//  Copyright (c) 2015 Stony Brook University. All rights reserved.
//

#import "AccAPI.h"
#import "ControlTypes.h"
#import "Sinter.h"
#import "Config.h"
//#import "IRTransformation.h"


static NSDictionary * serviceCodes;
static NSDictionary * roleMappings;

@implementation AccAPI

+ (void) initialize {
    serviceCodes = [Config getServiceCodes];
    roleMappings = [Config getRoleMappings];
}

+ (NSArray *) getAllProcessesIDs {
    CFArrayRef appList = CGWindowListCopyWindowInfo((kCGWindowListOptionOnScreenOnly), kCGNullWindowID);
    int num_process = (int) CFArrayGetCount(appList);
    NSLog(@"Process Count = %i", num_process);
    
    NSMutableArray * processes = [[NSMutableArray alloc] initWithCapacity:num_process];
    for (NSMutableDictionary* win in (__bridge NSArray*) appList){
        NSNumber* pid = [[NSNumber alloc] initWithInteger:[[win objectForKey:(id) kCGWindowOwnerPID] integerValue]];
        [processes addObject: pid];
    }
    
    if (appList) {
        CFRelease(appList);
    }
    return [processes copy];
}

+ (Entity *) getEntityForApp:(pid_t) pid {
    Entity * entity = nil;
    AXUIElementRef element = AXUIElementCreateApplication(pid);
    if (!element) {
        return entity;
    }
    
    entity = [[Entity alloc] init];
    [entity setProcess_id:[NSString stringWithFormat:@"%i", pid]];
    [entity setUnique_id: [self getCompleteIdOfUIElement:element havingIndex:0 andParentID:@""]];
    [entity setType     : [self getRoleOfUIElement:element]];
    [entity setName     : [self  getTitleOfUIElement:element]];
    [entity setValue    : [self getValueOfUIElement:element]];
    
    return entity;
}

// ls command
+ (Sinter *) getListOfApplications {
    Sinter * sinter = [[Sinter alloc] initWithApplications];
    sinter.header.service_code = [serviceCodes objectForKey:@"ls_res"];
    
    NSArray * processes =  [self getAllProcessesIDs];
    for ( NSNumber * process_id in processes){
        [sinter.applications addObject: [self getEntityForApp: (pid_t) [process_id integerValue]]];
    }
    return sinter;
}


// ls_l command
+ (Sinter *) getDomOf:(pid_t) pid andReturnRef:(AXUIElementRef*) elemRef withCache:(NSMutableDictionary*) cache {
    Sinter * sinter = [[Sinter alloc] initWithEntity];
    sinter.header.service_code = [serviceCodes objectForKey:@"ls_l_res"];
    sinter.header.process_id = [NSString stringWithFormat:@"%i", (int) pid];

    NSRect desktop = [[NSScreen mainScreen] frame];
    sinter.header.screen = [[Screen alloc] initWithHeight:(int)desktop.size.height andWidth:(int)desktop.size.width];
    
    
    AXUIElementRef element = AXUIElementCreateApplication(pid);
    
    Entity * root_entity = [self getEntityFroElement:element atIndex:0  havingId:nil andParentId:@"" withCache:cache updateCache:YES];
    sinter.entity = root_entity;
    
    //body = (NSXMLElement *) [IRTransformation performMenuTransfromationOn:body];
    //body = (NSXMLElement *) [IRTransformation performMenuTransfromation2On:body];
    //body = (NSXMLElement *)[IRTransformation lookAlike:body];
    
    //return data and appRef
    *elemRef = element;
    return sinter;
}


// delta command
+ (Sinter *) getDeltaAt:(AXUIElementRef) element havingPID:(pid_t) pid andUpdateType:(NSString *) updateType withCache:(NSMutableDictionary*) cache  {
    
    AXUIElementRef parent  = NULL;
    NSString * parent_key  = @"";
    NSString * current_key = @"";
    
    NSMutableArray *keys =[[NSMutableArray alloc] init];
    NSDictionary * dict_keys = [self getSegmentedIdOfUIElement:element andReturnKeys:&keys];
    
    BOOL hit = NO;
    for (int i = 0; i < [keys count]; i++) {
        parent_key = (NSString *)[keys lastObject];
        
        for(int j = (int)[keys count] - 2 ; j >= i ; j--){
            parent_key = [NSString stringWithFormat:@"%@/%@", parent_key, keys[j]];
        }
        if (i == 0) {
            current_key = parent_key;
        }
        if([cache objectForKey:parent_key]){
            parent = (__bridge AXUIElementRef)([dict_keys objectForKey: keys[i]]);
            hit    = YES;
            NSLog(@"got a hit %@ reason:%@ %@", parent_key, updateType, parent);
            break;
        }
    }
    
    if(!hit || !parent) return nil;
    
    Sinter * sinter = [[Sinter alloc] initWithUpdates];
    sinter.header.service_code = [serviceCodes objectForKey:@"delta"];
    sinter.header.process_id = [NSString stringWithFormat:@"%d", pid];
    sinter.header.kbd_or_action = [[KbdOrAction alloc] initWithTarget:parent_key andData:updateType];
    
    
    Entity * body = [self getEntityFroElement:element atIndex:0  havingId:parent_key andParentId:nil withCache:cache updateCache:NO];
    if(body) {
        [sinter.updates addObject:body];
    }
    
    return sinter;
}


+ (Entity *) getEntityFroElement:(AXUIElementRef) element atIndex:(int) index havingId:(NSString*) elemId andParentId:(NSString *) parentId withCache:(NSMutableDictionary*) cache updateCache:(bool) whenAsked {
    
    // getting id
    NSString * primary_id =  elemId;
    if( !elemId) {
        primary_id = [self getCompleteIdOfUIElement:element havingIndex:index andParentID:parentId];
    }
    
    // add to primary key dictionary for future lookup
    if (whenAsked) {
        if([cache objectForKey:primary_id]){
            NSLog(@"duplicated key %@", primary_id);
        }
        [cache setObject:[NSNumber numberWithBool:YES] forKey:primary_id];
    }
    
    Entity * entity     = [[Entity alloc] init];
    
    [entity setUnique_id: primary_id];
    [entity setType     : [self getRoleOfUIElement:element]];
    [entity setName     : [self getTitleOfUIElement:element]];
    [entity setValue    : [self getValueOfUIElement:element]];
    [entity setStates   : [NSNumber numberWithUnsignedInteger:[self getStatesOfUIElement:element]]];
    
    //NSRect rect   = [self flippedScreenBounds: [self getFrameOfUIElement:element]];
    NSRect rect   = [self getFrameOfUIElement:element];
    entity.top    = [NSNumber numberWithInt: rect.origin.y];
    entity.left   = [NSNumber numberWithInt: rect.origin.x];
    entity.height = [NSNumber numberWithInt: rect.size.height];
    entity.width  = [NSNumber numberWithInt: rect.size.width];
    
    
    // children
    int   child_count = [self getNumChildOfUIElement:element];
    entity.child_count = [NSNumber numberWithInt:child_count];
    
    CFArrayRef    children;
    if(child_count && AXUIElementCopyAttributeValue(element, (CFStringRef) NSAccessibilityChildrenAttribute, (CFTypeRef *) &children) == kAXErrorSuccess){
        
        entity.children = [[NSMutableArray alloc] init];
        for (int i = 0; i < CFArrayGetCount(children); i++){
            AXUIElementRef child_element = (AXUIElementRef) CFArrayGetValueAtIndex(children, i);
            [entity.children addObject:
             [self getEntityFroElement:child_element atIndex:i havingId:nil andParentId:primary_id withCache:cache updateCache:whenAsked]];
        }
        CFRelease(children);
    }
    return entity;
}

+ (NSArray *) attributeNamesOfUIElement:(AXUIElementRef)element {
    CFArrayRef attrNames;
    AXUIElementCopyAttributeNames(element, (CFArrayRef *)&attrNames);
    return (__bridge NSArray *)attrNames;
}

+ (id) valueOfAttribute:(NSString *)attribute ofUIElement:(AXUIElementRef) element {
    CFTypeRef result;
    if(AXUIElementCopyAttributeValue (element, (__bridge CFStringRef) attribute, (CFTypeRef *)&result) == kAXErrorSuccess ) {
        return (__bridge_transfer id) result;
    }
    return nil;
}

+ (NSString *) getRoleOfUIElement:(AXUIElementRef) element {
    NSString *role =  (__bridge NSString*) kAXUnknownRole;
    id data = [self valueOfAttribute:NSAccessibilityRoleAttribute ofUIElement:element];
    if(data){
        role = (NSString *) data;
    }
    NSString * generic_role = [roleMappings objectForKey:role];
    if(generic_role){
        return generic_role;
    } else {
        return role;
    }
}


typedef enum {
    INVALID    =  -1,
    PRIMARY   =  0,
    SECONDARY = 1,
    TERTIARY = 2
} id_type;

+ (NSString * ) getImmediateIdOfUIElement: (AXUIElementRef) element andReturnIdType:(int *) type {
    id result;
    NSString        *primary_key = @"";
    NSString        *role;
    *type = INVALID;
    
    role = [self getRoleOfUIElement:element];
    
    // primary: check if it's application root
    if([role isEqualToString:NSAccessibilityApplicationRole ]){
        pid_t pid;
        if (AXUIElementGetPid(element, &pid) == kAXErrorSuccess){
            primary_key = [NSString stringWithFormat:@"%i", pid];
        }else{
            primary_key = NSAccessibilityApplicationRole;
        }
        *type = PRIMARY;
        return primary_key;
    }
    
    // primary: check if ID field is defined
    if((result = [self valueOfAttribute:NSAccessibilityIdentifierAttribute ofUIElement:element])){
        primary_key = [NSString stringWithFormat:@"%@_%@", role, (NSString*) result];
        *type = PRIMARY;
        return primary_key;
    }
    
    // secondary: check if index is defined
    if((result = [self valueOfAttribute:NSAccessibilityIndexAttribute ofUIElement:element])){
        primary_key = [NSString stringWithFormat:@"%@_%@",role, (NSNumber*) result];
        *type = SECONDARY;
        return primary_key;
    }
    
    // tertiary: otherwise use 'role name_{index}'
    *type = TERTIARY;
    
    primary_key = role;
    return primary_key;
}

+ (NSString * ) getCompleteIdOfUIElement: (AXUIElementRef)element havingIndex:(int) index andParentID:(NSString *) parentID {
    id_type        type;
    NSString       *key ;
    NSString       *temp;
 
    temp = [self getImmediateIdOfUIElement:element andReturnIdType: &type];
    if (type == TERTIARY)
        key = [NSString stringWithFormat:@"%@/%@_%i",parentID, temp, index];
    else if(type == SECONDARY)
        key = [NSString stringWithFormat:@"%@/%@", parentID, temp];
    else if(type == PRIMARY)
        key = [NSString stringWithFormat:@"%@", temp];
    else{
        key = @"invalid id";
    }
    return key;
}


+ (NSDictionary *) getSegmentedIdOfUIElement: (AXUIElementRef) element andReturnKeys:(NSMutableArray**) key_list {
    id_type        type;
    int            index;
    NSString       *temp, *key;
    AXUIElementRef parent;

    NSMutableDictionary *key_dict = [[NSMutableDictionary alloc] init];
    do{
        temp = [self getImmediateIdOfUIElement:element andReturnIdType: &type];
        parent = (__bridge AXUIElementRef)[self valueOfAttribute:NSAccessibilityParentAttribute ofUIElement:element] ;
        if (parent){
            index = [self indexInParentofUIElement:element parent:parent];
            if (type == TERTIARY)
                key = [NSString stringWithFormat:@"%@_%i", temp, index];
            else
                key = [NSString stringWithFormat:@"%@", temp];
        }
        else{
            key = temp;
        }
        [key_dict setObject:(__bridge id)element forKey:key];
        [*key_list addObject:key];
        element = parent;
    } while(element != NULL && type != PRIMARY);
    
    return [key_dict copy];
}


+ (NSString *) getTitleOfUIElement:(AXUIElementRef) element {
    NSString *title = @"";
    CFTypeRef result;
    bool success = NO;
    if (AXUIElementCopyAttributeValue(element, (__bridge CFStringRef) NSAccessibilityTitleAttribute, (CFTypeRef *)&result) == kAXErrorSuccess
        && result){
        success = YES;
    }
    else if (AXUIElementCopyAttributeValue(element, (__bridge CFStringRef) NSAccessibilityTitleUIElementAttribute, (CFTypeRef *)&result) == kAXErrorSuccess
        && result){
        success = YES;

    }
    else if (AXUIElementCopyAttributeValue(element, (__bridge CFStringRef) NSAccessibilityDescriptionAttribute, (CFTypeRef *)&result) == kAXErrorSuccess
        && result){
        success = YES;
    }
    else if (AXUIElementCopyAttributeValue(element, (__bridge CFStringRef)NSAccessibilitySubroleAttribute, (CFTypeRef *)&result) == kAXErrorSuccess
        && result){
        success = YES;
    }
    else if (AXUIElementCopyAttributeValue(element, (__bridge CFStringRef)  NSAccessibilityHelpAttribute, (CFTypeRef *)&result) == kAXErrorSuccess
        && result){
        success = YES;
    }

    if(success) {
        if (CFGetTypeID(result) == CFStringGetTypeID()) {
            title = (__bridge NSString *) result;
            CFRelease( result);
            return title;
        }
    }

    return title;
}

+ (int) getNumChildOfUIElement:(AXUIElementRef) element {
    int num_child = 0;
    CFIndex result;

    if (AXUIElementGetAttributeValueCount (element, (__bridge CFStringRef) NSAccessibilityDisclosedRowsAttribute, &result) == kAXErrorSuccess
        && result){
        num_child = (int)result;
        return num_child;
    }

    
    if (AXUIElementGetAttributeValueCount(element, (__bridge CFStringRef) NSAccessibilityChildrenAttribute, &result) == kAXErrorSuccess
        && result){
        num_child = (int)result;
        return num_child;
    }
    if (AXUIElementGetAttributeValueCount (element, (__bridge CFStringRef) NSAccessibilityRowsAttribute, &result) == kAXErrorSuccess
        && result){
        num_child = (int)result;
        return num_child;
    }


    return num_child;
}

+ (NSString *) getValueOfUIElement:(AXUIElementRef) element {
    NSString *value = @"";
    CFTypeRef result;
    
    if (AXUIElementCopyAttributeValue(element, (__bridge CFStringRef) NSAccessibilityValueAttribute, (CFTypeRef *)&result) == kAXErrorSuccess
        && result){
        value = (__bridge NSString *) result;
        CFRelease( result);
        return value;
    }
    if (AXUIElementCopyAttributeValue(element, (__bridge CFStringRef) NSAccessibilityValueDescriptionAttribute, (CFTypeRef *)&result) == kAXErrorSuccess
        && result){
        value = (__bridge NSString *) result;
        CFRelease( result);
        return value;
    }
    return value;
}


+ (BOOL) canSetAttribute:(NSString *)attributeName ofUIElement:(AXUIElementRef)element {
    Boolean isSettable = false;
    AXUIElementIsAttributeSettable(element, (__bridge CFStringRef)attributeName, &isSettable);
    return (BOOL)isSettable;
}

+ ( unsigned int) getStatesOfUIElement:(AXUIElementRef)element {
    NSNumber *value ;
    id result;
    unsigned int states  = 0 ;
    // enabled
    if((result = [self valueOfAttribute:NSAccessibilityEnabledAttribute ofUIElement:element])){
        value = (NSNumber *) result;
        if ([value boolValue])
            states |=STATE_DEFAULT;
        else
            states |=STATE_DISABLED;
    }
    // focused
    if((result = [self valueOfAttribute:NSAccessibilityFocusedAttribute ofUIElement:element])){
        value = (NSNumber *) result;
        if ([value boolValue])
            states |=STATE_FOCUSED;
    }
    if((result = [self valueOfAttribute:NSAccessibilitySelectedAttribute ofUIElement:element])){
        value = (NSNumber *) result;
        if ([value boolValue])
            states |=STATE_SELECTED;
    }

    //hidden
    if(!([self valueOfAttribute:NSAccessibilityPositionAttribute ofUIElement:element] &&
     [self valueOfAttribute:NSAccessibilitySizeAttribute ofUIElement:element])){
        states |=STATE_INVISIBLE;
    }
    return states;
}


+ (CGPoint)carbonScreenPointFromCocoaScreenPoint:(NSPoint)cocoaPoint {
    NSScreen *foundScreen = nil;
    CGPoint thePoint;
    
    for (NSScreen *screen in [NSScreen screens]) {
        if (NSPointInRect(cocoaPoint, [screen frame])) {
            foundScreen = screen;
        }
    }
    if (foundScreen) {
        CGFloat screenHeight = [foundScreen frame].size.height;
        thePoint = CGPointMake(cocoaPoint.x, screenHeight - cocoaPoint.y - 1);
    } else {
        thePoint = CGPointMake(0.0, 0.0);
    }
    return thePoint;
}

+ (NSRect) flippedScreenBounds:(NSRect) bounds {
    NSRect desktop = [[NSScreen mainScreen] frame];
    bounds.origin.y = desktop.size.height - bounds.origin.y;
    return bounds;
}

+ (NSRect) getFrameOfUIElement:(AXUIElementRef)element {
    NSRect bounds = NSZeroRect;
    
    id elementPosition = [self valueOfAttribute:NSAccessibilityPositionAttribute ofUIElement:element];
    id elementSize = [self valueOfAttribute:NSAccessibilitySizeAttribute ofUIElement:element];
    
    if (elementPosition && elementSize) {
        NSRect topLeftWindowRect;
        AXValueGetValue((__bridge AXValueRef)elementPosition, kAXValueCGPointType, &topLeftWindowRect.origin);
        AXValueGetValue((__bridge AXValueRef)elementSize, kAXValueCGSizeType, &topLeftWindowRect.size);
        //bounds = [self flippedScreenBounds:topLeftWindowRect];
        bounds = topLeftWindowRect;
    }
    return bounds;
}


+(int) indexInParentofUIElement:(AXUIElementRef) element parent:(AXUIElementRef) parent {
    CFIndex index = kCFNotFound;
    if (parent) {
        int           child_count = 0;
        CFArrayRef    children;
        child_count = [self getNumChildOfUIElement:parent];
        if (child_count == 1) {
            return 0;
        }
        if(child_count > 1 &&
           AXUIElementCopyAttributeValue(parent, (CFStringRef) NSAccessibilityChildrenAttribute, (CFTypeRef *) &children) == kAXErrorSuccess){
            CFRange range = {0, child_count};
            index = CFArrayGetFirstIndexOfValue(children, range, element);
            CFRelease(children);
        }
    }
    return (int) index;
}


// error code descriptor
+ (void) printObserverStatus:(AXError) code {
    switch (code) {
        case kAXErrorSuccess:
            NSLog(@"Observer added successfully");
            break;
        case kAXErrorInvalidUIElementObserver:
            NSLog(@"kAXErrorInvalidUIElementObserver");
            break;
        case kAXErrorIllegalArgument:
            NSLog(@"kAXErrorIllegalArgument");
            break;
        case kAXErrorNotificationUnsupported:
            NSLog(@"kAXErrorNotificationUnsupported");
            break;
        case kAXErrorNotificationAlreadyRegistered:
            NSLog(@"kAXErrorNotificationAlreadyRegistered");
            break;
        case kAXErrorCannotComplete:
            NSLog(@"kAXErrorCannotComplete");
            break;
        case kAXErrorFailure:
            NSLog(@"kAXErrorFailure");
            break;
        default:
            NSLog(@"Observer does not added for unknown reason");
    }
}



@end
