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
    AccAPI.m
    OSXScrapper

    Created by Syed Masum Billah on 10/9/15.
*/
#import <Appkit/Appkit.h>
#import "AccAPI.h"
#import "ControlTypes.h"
#import "Sinter.h"
#import "Config.h"
#import "XMLTags.h"
#import "Header.h"
#import "Params.h"

//#import "IRTransformation.h"


static NSDictionary * roleMappings;
static NSMutableArray* valid_apps;

@implementation AccAPI

+ (void) initialize {
    roleMappings = [Config getRoleMappings];
    
    NSDictionary *settings = [NSDictionary dictionaryWithContentsOfFile:[[NSBundle mainBundle] pathForResource:@"Settings" ofType:@"plist"]];
    valid_apps = [settings objectForKey:@"support_apps"];
}

+ (BOOL) isUnitTesting {
    NSDictionary* environment = [[NSProcessInfo processInfo] environment];
    return (environment[@"XCTestConfigurationFilePath"] != nil);
}

+ (NSArray *) getAllProcessesIDs {
    CFArrayRef appList = CGWindowListCopyWindowInfo((kCGWindowListOptionOnScreenOnly), kCGNullWindowID);
    int num_process = (int) CFArrayGetCount(appList);
    
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
        return nil;
    }
    
    if([[self getRoleOfUIElement:element] isEqualToString:@"application"]){
        //NSLog(@"pid %i adding to entity", pid);
    }
    else{
        return nil;
    }
    
    entity = [[Entity alloc] init];
    [entity setProcess_id:[NSString stringWithFormat:@"%i", pid]];
    [entity setUnique_id: [self getCompleteIdOfUIElement:element havingIndex:0 andParentID:@""]];
    [entity setType     : [self getRoleOfUIElement:element]];
    [entity setName     : [self getTitleOfUIElement:element]];
    [entity setValue    : [self getValueOfUIElement:element]];
    
    CFRelease(element);
    return entity;
}

+ (void) addValidApp: (NSString*) appName {
    if(![valid_apps containsObject:appName]){
        [valid_apps addObject:appName];
        NSLog(@"%s valid apps: %@", __PRETTY_FUNCTION__, valid_apps);
    }
}

// ls command
+ (Sinter *) getListOfApplications {
    Sinter * sinter = [[Sinter alloc] initWithEntities];
    sinter.header.service_code = [serviceCodes objectForKey:STRLsRes];
    sinter.header.sub_code = [serviceCodes objectForKey:STRLsRes];
    
    NSArray * processes =  [self getAllProcessesIDs];
    
    int nProcessAbleToSee = 0;
    for ( NSNumber * process_id in processes){
        Entity * e = [self getEntityForApp: (pid_t) [process_id integerValue]];
        if(e != nil){
            nProcessAbleToSee ++;
            if ([AccAPI isUnitTesting]) NSLog(@"%s ableToSee: %@", __PRETTY_FUNCTION__, e.name);
            if([valid_apps containsObject:e.name]) {
                [sinter.entities addObject:e];
            }
        }
    }
    if ([AccAPI isUnitTesting]) NSLog(@"%s nProcessAbleToSee = %i", __PRETTY_FUNCTION__, nProcessAbleToSee);
    return sinter;
}


// ls_l command
+ (Sinter *) getDomOf:(pid_t) pid andReturnRef:(AXUIElementRef*) elemRef withCache:(NSMutableDictionary*) cache {
    Sinter * sinter = [[Sinter alloc] initWithEntity];
    sinter.header.service_code = [serviceCodes objectForKey:STRLsLongRes];
    sinter.header.process_id = [NSString stringWithFormat:@"%i", (int) pid];

    NSRect desktop = [[NSScreen mainScreen] frame];
    Params * param = [[Params alloc] init];
    param.data1 = [NSNumber numberWithInt:(int)desktop.size.width].stringValue;
    param.data2 = [NSNumber numberWithInt:(int)desktop.size.height].stringValue;
    sinter.header.params = param;
    
    AXUIElementRef app = AXUIElementCreateApplication(pid);
        
    /* instead of sending the whole app in entity, sending frontwindow in entity and rests in entities (such as menubar) */
    AXUIElementRef frontWindow = NULL;
    AXError err = AXUIElementCopyAttributeValue(app, (CFStringRef)NSAccessibilityMainWindowAttribute, (CFTypeRef *)&frontWindow );
    if ( err == kAXErrorSuccess ){
        //getEntityFroElement is a recursive call by which we got the whole DOM tree under the frontWindow
        Entity * root_entity = [self getEntityFroElement:frontWindow atIndex:0  havingId:nil andParentId:@"" withCache:cache updateCache:YES];
        sinter.entity = root_entity;
    }
    else{
        sinter.entity = nil;
        return sinter;
    }


    int   child_count = [self getNumChildOfUIElement:app];
    CFArrayRef    children;
    if(child_count > 1 && AXUIElementCopyAttributeValue(app, (CFStringRef) NSAccessibilityChildrenAttribute, (CFTypeRef *) &children) == kAXErrorSuccess){
        sinter.entities = [[NSMutableArray alloc] init];
        for (int i = 0; i < CFArrayGetCount(children); i++){
            AXUIElementRef child_element = (AXUIElementRef) CFArrayGetValueAtIndex(children, i);
            
            CFTypeRef flagIsMainWindow;
            AXError err = AXUIElementCopyAttributeValue(child_element, (CFStringRef)NSAccessibilityMainAttribute, (CFTypeRef *)&flagIsMainWindow);
            if(err == kAXErrorSuccess && [(__bridge NSNumber *)flagIsMainWindow boolValue]){
                NSLog(@"not sending windows again");
            }
            else
            [sinter.entities addObject:
             [self getEntityFroElement:child_element atIndex:i havingId:nil andParentId:@"/application_0" withCache:cache updateCache:YES]];
        }
        CFRelease(children);
    }
    
    
    //body = (NSXMLElement *) [IRTransformation performMenuTransfromationOn:body];
    //body = (NSXMLElement *) [IRTransformation performMenuTransfromation2On:body];
    //body = (NSXMLElement *)[IRTransformation lookAlike:body];
    
    //return data and appRef
    *elemRef = app;
    
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
            parent_key = [NSString stringWithFormat:@"/%@/%@", parent_key, keys[j]]; //should be "/application_0/menubar_1" instead of "application_0/menubar_1"
        }
        if (i == 0) {
            current_key = parent_key;
        }
        //parent_key = [NSString stringWithFormat:@"/%@", parent_key];
        if([cache objectForKey:parent_key]){
            parent = (__bridge AXUIElementRef)([dict_keys objectForKey: keys[i]]);
            hit    = YES;
            //NSLog(@"got a hit %@ reason:%@ %@", parent_key, updateType, parent);
            NSLog(@"updateType:%@ for %@", updateType, parent_key);
            break;
        }
    }
    
    if(!hit || !parent) return nil;
    
    if([updateType isEqualToString:(NSString*)kAXValueChangedNotification]){
    
        NSNumber * service_code = [serviceCodes objectForKey:STRDelta];
        NSNumber * sub_code = [serviceCodes objectForKey:STRDeltaPropValueChanged];
        Params * params = [[Params alloc] init];
        params.target_id = parent_key;
        params.data2 = [self getValueOfUIElement:element];
        
        Sinter * sinter = [[Sinter alloc] initWithServiceCode:service_code
                                                      subCode:sub_code
                                                    processId:[NSString stringWithFormat:@"%d", pid]
                                                       params:params];
        return sinter;
        
      
    }
    else if ([updateType isEqualToString:(NSString*)kAXSelectedChildrenChangedNotification]){
        NSNumber * service_code = [serviceCodes objectForKey:STRDelta];
        NSNumber * sub_code = [serviceCodes objectForKey:STRDeltaSubtreeExpand];
        Sinter * sinter = [[Sinter alloc] initWithServiceCode:service_code
                                                      subCode:sub_code
                                                    processId:[NSString stringWithFormat:@"%d", pid]
                                                       params:nil];
        sinter.entity = [self getEntityFroElement:element atIndex:0 havingId:parent_key andParentId:nil withCache:cache updateCache:NO];
        return sinter;
    }
    else{
        NSLog(@"updateType %@ not handled yet", updateType);
    }
    
    /*
     Entity * body = [self getEntityFroElement:element atIndex:0  havingId:parent_key andParentId:nil withCache:cache updateCache:NO];
     if(body) {
     [sinter.entities addObject:body];
     }
     */
    return nil;
}


+ (Entity *) getEntityFroElement:(AXUIElementRef) element atIndex:(int) index havingId:(NSString*) elemId andParentId:(NSString *) parentId withCache:(NSMutableDictionary*) cache updateCache:(bool) whenAsked {
    
    // getting id
    NSString * primary_id =  elemId;
    if( !elemId) {
        primary_id = [self getCompleteIdOfUIElement:element havingIndex:index andParentID:parentId];
    }
    
    NSString * name = [self getTitleOfUIElement:element];
    
    //do not send Apple system menu to client
    if ([name isEqualToString:@"Apple"]){
        return nil;
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

    if ([entity.type isEqualToString:@"button"]
        && ([entity.name isEqualToString:@""] && [entity.value isEqualToString:@""])){
        //NSLog(@"Some Buttons are without name and value, get label"); //for example in calculator app
        [entity setName    : [self getAccessbilityDescriptionAttribute:element]];
    }
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
            Entity * e = [self getEntityFroElement:child_element atIndex:i havingId:nil andParentId:primary_id withCache:cache updateCache:whenAsked];
            if (e != nil)[entity.children addObject:e];
        }
        CFRelease(children);
    }
    return entity;
}

+ (void) bringWindowToFront:(int)pid {
    AXUIElementRef app = AXUIElementCreateApplication(pid);
    int   child_count = [self getNumChildOfUIElement:app];
    CFArrayRef    children;
    NSString * app_unique_id = [self getCompleteIdOfUIElement:app havingIndex:0 andParentID:@""];

    if(child_count && AXUIElementCopyAttributeValue(app, (CFStringRef) NSAccessibilityChildrenAttribute, (CFTypeRef *) &children) == kAXErrorSuccess) {
        for (int i = 0; i < CFArrayGetCount(children); i++){
            AXUIElementRef child_element = (AXUIElementRef) CFArrayGetValueAtIndex(children, i);
            NSString * unique_id = [self getCompleteIdOfUIElement:child_element havingIndex:i andParentID:app_unique_id];
            if([unique_id hasPrefix:@"window"]) {
                AXError ret = AXUIElementPerformAction(child_element, (CFStringRef)@"AXRaise");
                NSLog(@"Action raise window, result = %d", ret);
                CFRelease(app);
                return;
            }
        }
    }
    CFRelease(app);
}

+ (AXUIElementRef) findAXUIElement:(NSString *)unique_id root:(AXUIElementRef)root atIndex:(int)index andParentId:(NSString *)parentId {
    
    // getting id
    NSString * primary_id = [self getCompleteIdOfUIElement:root havingIndex:index andParentID:parentId];
    if([unique_id isEqualToString:primary_id] ){
        return root;
    }
    
    // children
    int   child_count = [self getNumChildOfUIElement:root];
    CFArrayRef    children;
    if(child_count && AXUIElementCopyAttributeValue(root, (CFStringRef) NSAccessibilityChildrenAttribute, (CFTypeRef *) &children) == kAXErrorSuccess){
        for (int i = 0; i < CFArrayGetCount(children); i++){
             AXUIElementRef child_element = (AXUIElementRef) CFArrayGetValueAtIndex(children, i);
            AXUIElementRef element = [self findAXUIElement:unique_id root:child_element atIndex:i andParentId:primary_id];
            if(element != nil){
                return element;
            }
        }
    }
    return nil;
}

+ (void) handleActionDefault:(int)pid targetID:(NSString*)whichUI {
    
    AXUIElementRef app = AXUIElementCreateApplication(pid);
    AXUIElementRef ui = [AccAPI findAXUIElement:whichUI root:app atIndex:0 andParentId:@""];
    AXError ret = 0;
    NSString * defaultActionName = nil;
    
    [self bringWindowToFront:pid];
    
    /* for development logging: to know which actions are there */
    CFArrayRef actionNames;
    ret = AXUIElementCopyActionNames(ui, (CFArrayRef *)&actionNames);
    if(actionNames != nil){
        for(int i=0; i<CFArrayGetCount(actionNames);i++) {
            NSLog(@"actionNames[%d] = %@", i, (CFStringRef)CFArrayGetValueAtIndex(actionNames, i));
        }
    }
    
    /* decide what's the default action for this UI type */
    NSString * type =[AccAPI getRoleOfUIElement:ui];
    if([type isEqualToString:@"button"] || [type isEqualToString:@"menuitem"]){
        defaultActionName = @"AXPress";
    }
 
    NSLog(@"handleActionDefault() for \"%@\", action: %@", whichUI, defaultActionName);
    ret = AXUIElementPerformAction(ui, (CFStringRef)defaultActionName);
    if(ret != kAXErrorSuccess){
        NSLog(@"Action %@ result = %d",defaultActionName, ret);
    }
    
    CFRelease(app);
}

+ (void) handleActionExpand:(int)pid targetID:(NSString*)whichUI {
    
    AXUIElementRef app = AXUIElementCreateApplication(pid);
    AXUIElementRef ui = [AccAPI findAXUIElement:whichUI root:app atIndex:0 andParentId:@""];
    AXError ret = 0;
    NSString * defaultActionName = nil;
    
    [self bringWindowToFront:pid];
    
    /* for development logging: to know which actions are there */
    /*
    CFArrayRef actionNames;
    ret = AXUIElementCopyActionNames(ui, (CFArrayRef *)&actionNames);
    if(actionNames != nil){
        for(int i=0; i<CFArrayGetCount(actionNames);i++) {
            NSLog(@"actionNames[%d] = %@", i, (CFStringRef)CFArrayGetValueAtIndex(actionNames, i));
        }
    }
    */
    
    /* decide what's the default action for this UI type */
    NSString * type =[AccAPI getRoleOfUIElement:ui];
    if([type isEqualToString:@"menubaritem"] ){
        defaultActionName = @"AXPress";
    }
    
    NSLog(@"handleActionExpand() for \"%@\", action: %@", whichUI, defaultActionName);
    ret = AXUIElementPerformAction(ui, (CFStringRef)defaultActionName);
    if(ret != kAXErrorSuccess){
        NSLog(@"Action %@ result = %d",defaultActionName, ret);
    }
    
    CFRelease(app);
}

+ (void) handleActionCollapse:(int)pid targetID:(NSString*)whichUI {
    
    AXUIElementRef app = AXUIElementCreateApplication(pid);
    AXUIElementRef ui = [AccAPI findAXUIElement:whichUI root:app atIndex:0 andParentId:@""];
    AXError ret = 0;
    NSString * defaultActionName = nil;
    
    /* for development logging: to know which actions are there */
    /*
    CFArrayRef actionNames;
    ret = AXUIElementCopyActionNames(ui, (CFArrayRef *)&actionNames);
    if(actionNames != nil){
        for(int i=0; i<CFArrayGetCount(actionNames);i++) {
            NSLog(@"actionNames[%d] = %@", i, (CFStringRef)CFArrayGetValueAtIndex(actionNames, i));
        }
    }
    */
    
    /* decide what's the default action for this UI type */
    NSString * type =[AccAPI getRoleOfUIElement:ui];
    if([type isEqualToString:@"menubaritem"] ){
        defaultActionName = @"AXCancel";
    }

    NSLog(@"handleActionCollapse() for \"%@\", action: %@", whichUI, defaultActionName);
    ret = AXUIElementPerformAction(ui, (CFStringRef)defaultActionName);
    if(ret != kAXErrorSuccess){
        NSLog(@"Action %@ result = %d",defaultActionName, ret);
    }
    
    CFRelease(app);
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
        //primary_key = (NSString*) result;  //somehow modify this will cuz main display disappear
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
    if (type == TERTIARY){
        key = [NSString stringWithFormat:@"%@/%@_%i",parentID, temp, index];
    }
    else if(type == SECONDARY){
        key = [NSString stringWithFormat:@"%@/%@", parentID, temp];
        NSLog(@"type = %d: unique_id = %@", type, key);
    }
    else if(type == PRIMARY){
        key = [NSString stringWithFormat:@"%@", temp];
    }
    else{
        key = @"invalid id";
    }
    
    //NSLog(@"type = %d: unique_id = %@", type, key);
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
            if (type == TERTIARY)
                key = [NSString stringWithFormat:@"%@_%i", temp, 0]; //should be application_0 instead of application
            else
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
    AXError ret = kAXErrorSuccess;
    
    ret = AXUIElementCopyAttributeValue(element, (__bridge CFStringRef) NSAccessibilityValueAttribute, (CFTypeRef *)&result);
    //NSLog(@"AXUIElementCopyAttributeValue, NSAccessibilityValueAttribute ret = %d", ret);
    //if (ret == kAXErrorSuccess && result){
    if (ret == kAXErrorSuccess && CFGetTypeID(result) == CFStringGetTypeID()){
        value = (__bridge NSString *) result;
        CFRelease( result);
        return value;
    }

    ret = AXUIElementCopyAttributeValue(element, (__bridge CFStringRef) NSAccessibilityValueDescriptionAttribute, (CFTypeRef *)&result);
    //NSLog(@"AXUIElementCopyAttributeValue, NSAccessibilityValueDescriptionAttribute ret = %d", ret);
    if (ret == kAXErrorSuccess && result){
        value = (__bridge NSString *) result;
        CFRelease( result);
        return value;
    }
    return value;
}

+ (NSString *) getAccessbilityDescriptionAttribute:(AXUIElementRef) element {
    NSString *value = @"";
    CFTypeRef result;
    AXError ret = kAXErrorSuccess;
    
    ret = AXUIElementCopyAttributeValue(element, (__bridge CFStringRef) NSAccessibilityDescriptionAttribute, (CFTypeRef *)&result);
    if (ret == kAXErrorSuccess && result){
        value = (__bridge NSString *) result;
        CFRelease( result);
        return value;
    }
    else{
        //NSLog(@"AXUIElementCopyAttributeValue, NSAccessibilityDescriptionAttribute ret = %d", ret);
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
